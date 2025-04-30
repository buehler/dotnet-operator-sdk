# Implementing Controller Logic

Controllers are the heart of a Kubernetes operator. They contain the business logic that watches for changes to specific resources (usually your [Custom Resources](https://kubernetes.io/docs/concepts/extend-kubernetes/api-extension/custom-resources/), but potentially built-in ones too) and takes action to ensure the actual state of the system matches the desired state defined in the resource's `spec`.

This process is often referred to as the [**reconciliation loop**](https://kubernetes.io/docs/concepts/architecture/controller/#controller-pattern).

In KubeOps, controllers are implemented by creating classes that implement the `IResourceController<TEntity>` interface from the `KubeOps.Operator.Controller` namespace.

## The `IResourceController<TEntity>` Interface

This interface defines the contract for a controller that handles a specific entity type (`TEntity`).

```csharp
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using Microsoft.Extensions.Logging;
using MyFirstOperator.Entities; // Assuming your entity is defined here

public class V1Alpha1DemoEntityController : IResourceController<V1Alpha1DemoEntity>
{
    private readonly ILogger<V1Alpha1DemoEntityController> _logger;

    public V1Alpha1DemoEntityController(ILogger<V1Alpha1DemoEntityController> logger)
    {
        _logger = logger;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(V1Alpha1DemoEntity entity)
    {
        _logger.LogInformation($"Reconciling entity {entity.Name} in namespace {entity.Namespace()}.");

        // --- Your Reconciliation Logic Goes Here ---
        // 1. Check the current state of the world (e.g., related Pods, Services, ConfigMaps)
        // 2. Compare with the desired state in entity.Spec
        // 3. Create/Update/Delete resources to match the desired state
        // 4. Update the entity.Status if necessary
        // ------------------------------------------

        // Example: Update the status based on reconciliation
        // (Actual API call to update status shown in the 'Using the Kubernetes Client' section below)
        if (entity.Status.ObservedMessage != entity.Spec.Message)
        {
            entity.Status.ObservedMessage = entity.Spec.Message;
            entity.Status.State = "Processed";
            entity.Status.LastUpdated = DateTime.UtcNow;

            // Indicate that the status subresource needs to be updated
            // The KubernetesClient (injected or used directly) handles the API call.
            // This example assumes you have a method/service to do this.
            // await _kubernetesClient.UpdateStatus(entity);

            _logger.LogInformation($"Updated status for {entity.Name}.");
        }

        // Return null or a ResourceControllerResult to requeue
        return null; // Returning null means reconciliation completed successfully for now
    }

    public Task StatusModifiedAsync(V1Alpha1DemoEntity entity)
    {
        _logger.LogInformation($"Status updated for entity {entity.Name}.");
        // Usually, no action is needed here unless the status change itself triggers further reconciliation.
        return Task.CompletedTask;
    }

    public Task DeletedAsync(V1Alpha1DemoEntity entity)
    {
        _logger.LogInformation($"Entity {entity.Name} deleted.");
        // Called when the entity is deleted. 
        // Note: Finalizers should be used for complex cleanup before deletion occurs.
        return Task.CompletedTask;
    }
}
```

Key elements:

*   **`TEntity`**: The specific type of custom resource this controller manages (e.g., `V1Alpha1DemoEntity`). It must match the type defined in [Defining Custom Entities](./custom-entities.md).
*   **Dependency Injection**: Controllers are registered in the [.NET Dependency Injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) container. You can inject services like loggers (`ILogger<T>`), the Kubernetes client (`IKubernetesClient`), or your own services.

## The `ReconcileAsync` Method

This is the most important method. It's called by the KubeOps runtime whenever:

1.  A resource of type `TEntity` is **created**.
2.  A resource of type `TEntity` is **updated** (specifically, if its `metadata.generation` field increments, indicating a change to the `spec`).
3.  The operator **restarts** (it will reconcile all existing resources).
4.  A **requeue** is requested (see below).

The `entity` parameter contains the full custom resource object, including its `metadata`, `spec`, and current `status`.

**Your primary task within `ReconcileAsync` is to make the state of the world match the `entity.Spec`.** This often involves:

*   Reading other Kubernetes resources (Pods, Services, ConfigMaps, Secrets, other CRs).
*   Comparing the desired state (`entity.Spec`) with the actual state.
*   Creating, updating, or deleting associated Kubernetes resources using the Kubernetes client.
*   Updating the `entity.Status` to reflect the observed state.

**Return Value:**

*   **`null` or `Task.FromResult<ResourceControllerResult?>(null)`**: Indicates successful reconciliation for now. The resource won't be automatically requeued unless it's updated again.
*   **`ResourceControllerResult.RequeueEvent(TimeSpan delay)`**: Tells KubeOps to call `ReconcileAsync` again for this specific resource after the specified delay. Useful if a temporary condition prevents successful reconciliation (e.g., waiting for another resource to be ready) or if periodic reconciliation is needed.
*   **Throwing an Exception**: Indicates a fatal error during reconciliation. The event might be retried by KubeOps with backoff, but persistent errors should be investigated.

## Other Interface Methods

*   **`StatusModifiedAsync(TEntity entity)`**: Called *only* when the `status` subresource of the entity is updated. This is less common to implement, as most logic reacts to `spec` changes handled in `ReconcileAsync`.
*   **`DeletedAsync(TEntity entity)`**: Called when the entity is observed as deleted by the Kubernetes API server *after* any finalizers have completed. Use this for simple cleanup tasks. For complex cleanup that must happen *before* the resource is removed from the API, use [Finalizers](./finalizers.md) instead.

## Watching Related Resources (Advanced)

While controllers primarily watch their associated `TEntity`, sometimes the reconciliation logic depends on changes to *other* Kubernetes resources (e.g., a Secret containing configuration, a Deployment managed by the operator). 

KubeOps allows controllers to watch related resources. When a watched related resource changes, it can trigger the reconciliation (`ReconcileAsync`) of the *owner* custom resource.

This is typically configured during operator startup using the `OperatorBuilder` and involves specifying how to map events from the related resource back to the owner custom resource instance(s).

For detailed information on how to configure watchers and map events back to your controller's reconciliation loop, please see the [Watching Related Resources](./watchers.md) documentation.

## Using the Kubernetes Client

To interact with the Kubernetes API (e.g., create a Pod, get a Service, update a ConfigMap), you need an instance of the Kubernetes client. KubeOps provides an enhanced client interface `KubeOps.KubernetesClient.IKubernetesClient` which builds upon the official `k8s.Kubernetes` client.

Inject `IKubernetesClient` into your controller via the constructor:

```csharp
using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using Microsoft.Extensions.Logging;
using MyFirstOperator.Entities;

public class V1Alpha1DemoEntityController : IResourceController<V1Alpha1DemoEntity>
{
    private readonly ILogger<V1Alpha1DemoEntityController> _logger;
    private readonly IKubernetesClient _client;

    // Inject the client via constructor
    public V1Alpha1DemoEntityController(ILogger<V1Alpha1DemoEntityController> logger, IKubernetesClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(V1Alpha1DemoEntity entity)
    {
        _logger.LogInformation($"Reconciling entity {entity.Name} in namespace {entity.Namespace()}.");

        // Example: Create or Update a ConfigMap based on the entity's spec
        var configMap = new V1ConfigMap
        {
            Metadata = new V1ObjectMeta
            {
                Name = $"{entity.Name}-config",
                NamespaceProperty = entity.Namespace(),
                // Set owner reference so the ConfigMap is garbage collected when the entity is deleted
                OwnerReferences = new List<V1OwnerReference> { entity.CreateOwnerReference() }
            },
            Data = new Dictionary<string, string>
            {
                { "message", entity.Spec.Message },
                { "replicas", entity.Spec.Replicas.ToString() },
            }
        };

        try
        {
            // Use the client to create or update the resource
            var existingConfigMap = await _client.GetAsync<V1ConfigMap>(configMap.Metadata.Name, configMap.Metadata.NamespaceProperty);
            if (existingConfigMap != null)
            {
                // Ensure the resource version matches for the update
                configMap.Metadata.ResourceVersion = existingConfigMap.Metadata.ResourceVersion;
                await _client.UpdateObject(configMap);
                _logger.LogInformation($"Updated ConfigMap {configMap.Name()} for entity {entity.Name}.");
            }
            else
            {
                await _client.CreateAsync(configMap);
                _logger.LogInformation($"Created ConfigMap {configMap.Name()} for entity {entity.Name}.");
            }
        }
        catch (KubernetesException e)
        {
            _logger.LogError(e, $"Error creating/updating ConfigMap for {entity.Name}.");
            // Decide if this error warrants a requeue
            // return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(15));
            throw; // Or rethrow if it's a permanent issue
        }

        // Example: Update the entity status
        entity.Status.ObservedMessage = entity.Spec.Message;
        entity.Status.State = "ConfigMapUpdated";
        entity.Status.LastUpdated = DateTime.UtcNow;
        try
        {
            // Use the client to update the status subresource
            await _client.UpdateStatus(entity);
            _logger.LogInformation($"Updated status for {entity.Name}.");
        }
        catch (KubernetesException e)
        {
            _logger.LogError(e, $"Error updating status for {entity.Name}.");
            // Status updates often conflict if multiple reconciles happen quickly.
            // Requeuing might be appropriate here.
            return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(5));
        }

        return null;
    }
    // StatusModifiedAsync and DeletedAsync omitted for brevity
    public Task StatusModifiedAsync(V1Alpha1DemoEntity entity) => Task.CompletedTask;
    public Task DeletedAsync(V1Alpha1DemoEntity entity) => Task.CompletedTask;
}
```

The `IKubernetesClient` offers methods like:

*   `GetAsync<TEntity>(string name, string? ns = null)`: Retrieves a specific resource by name and optional namespace. Returns `null` if not found.
*   `ListAsync<TEntity>(string? ns = null, string? labelSelector = null)`: Lists resources of a specific type, optionally filtered by namespace or label selector.
*   `CreateAsync<TEntity>(TEntity obj)`: Creates a new resource in the cluster.
*   `UpdateObject<TEntity>(TEntity obj)`: Updates an existing resource. The resource must already exist.
*   `UpdateStatus<TEntity>(TEntity obj)`: Updates only the `status` subresource of an existing resource. Use this for status updates to avoid conflicts with spec changes.
*   `DeleteObject<TEntity>(TEntity obj)` or `DeleteObject<TEntity>(string name, string? ns = null)`: Deletes a resource, either by providing the object instance or by name and optional namespace.

**Important:** Always consider setting [Owner References](https://kubernetes.io/docs/concepts/overview/working-with-objects/owners-dependents/) (using the `entity.CreateOwnerReference()` helper) on resources created by your controller. This ensures Kubernetes automatically garbage collects the dependent resources (like the ConfigMap above) when the owner (your `V1Alpha1DemoEntity`) is deleted.

## Handling Errors and Requeuing

Reconciliation logic might fail due to various reasons:

*   Temporary network issues connecting to the Kubernetes API.
*   Required resources (like a Secret or another CRD) not being ready yet.
*   Rate limiting by the API server.
*   Conflicts during updates (e.g., trying to update a resource that was just modified).
*   Bugs in your reconciliation logic.

**Strategies:**

1.  **Catch Specific Exceptions:** Wrap API calls in `try...catch` blocks, specifically catching `k8s.KubernetesException` for API-related errors.
2.  **Logging:** Log errors clearly with relevant details (resource name, namespace, error message).
3.  **Requeuing:** If an error is likely temporary (e.g., network issue, dependency not ready, conflict on update), return `ResourceControllerResult.RequeueEvent(TimeSpan delay)` from `ReconcileAsync`. This tells KubeOps to try reconciling the same resource again after the specified `delay`. Choose a reasonable delay (e.g., 5s, 15s, 30s) and consider implementing exponential backoff for repeated failures (though KubeOps provides some level of this internally).
4.  **Don't Requeue Permanent Errors:** If an error is clearly due to invalid configuration in the `spec` or a bug, requeuing won't help. Log the error and return `null` or rethrow the exception. Rely on updates to the resource or operator restarts to trigger reconciliation again.
5.  **Status Updates:** Use the `status` subresource of your entity to report errors or progress back to the user. Update the status to indicate failure states.

```csharp
    public async Task<ResourceControllerResult?> ReconcileAsync(V1Alpha1DemoEntity entity)
    {
        try 
        { 
            // ... Main reconciliation logic ...

            // If successful, update status to reflect success
            entity.Status.State = "Ready";
            await _client.UpdateStatus(entity);
            return null; // Success!
        }
        catch (SomeTransientException e)
        {
            _logger.LogWarning(e, $"Transient error reconciling {entity.Name}. Requeuing.");
            // Update status to indicate transient failure
            entity.Status.State = "Reconciling"; 
            // Best effort status update, might fail too
            try { await _client.UpdateStatus(entity); } catch { /* Ignore */ }
            // Requeue the event
            return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(15));
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Unhandled error reconciling {entity.Name}. Not requeuing.");
             // Update status to indicate permanent failure
            entity.Status.State = "Error"; 
            try { await _client.UpdateStatus(entity); } catch { /* Ignore */ }
            // Do not requeue automatically, let updates/restarts trigger next attempt
            return null; 
        }
    }

```

Remember to register your controller implementation in your application's service collection, typically in `Program.cs`.

## Example

See a practical example of a controller implementation here:
[`examples/Operator/Controller/`](../examples/Operator/Controller/)
