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
    private readonly IKubernetesClient _client; // Assume IKubernetesClient is injected

    public V1Alpha1DemoEntityController(ILogger<V1Alpha1DemoEntityController> logger, IKubernetesClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(V1Alpha1DemoEntity entity)
    {
        _logger.LogInformation($"Reconciling entity {entity.Name} in namespace {entity.Namespace()}.");

        // --- Reconciliation Logic: Manage a Deployment --- 

        // 1. Define the Desired Deployment based on the entity's spec
        var desiredDeployment = new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = $"{entity.Name}-deployment", // Deployment name derived from CR name
                NamespaceProperty = entity.Namespace(),
                // **IMPORTANT**: Set owner reference so the Deployment is automatically
                // garbage collected by Kubernetes when the V1Alpha1DemoEntity is deleted.
                OwnerReferences = new List<V1OwnerReference> { entity.CreateOwnerReference() }
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = entity.Spec.Replicas, // Use replicas from CR spec
                Selector = new V1LabelSelector { MatchLabels = new Dictionary<string, string> { { "app", entity.Name } } },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta { Labels = new Dictionary<string, string> { { "app", entity.Name } } },
                    Spec = new V1PodSpec
                    {
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = "app-container",
                                Image = entity.Spec.Image, // Use image from CR spec
                                // Ports, env vars, volumes, etc., would go here
                            }
                        }
                    }
                }
            }
        };

        _logger.LogInformation($"Desired state: Deployment '{desiredDeployment.Name()}' with {desiredDeployment.Spec.Replicas} replicas and image '{desiredDeployment.Spec.Template.Spec.Containers[0].Image}'.");

        // 2. Get the current state of the Deployment
        V1Deployment? existingDeployment = null;
        try
        {
            existingDeployment = await _client.GetAsync<V1Deployment>(desiredDeployment.Metadata.Name, desiredDeployment.Metadata.NamespaceProperty);
        }
        catch (KubernetesException e) when (e.Status.Code == 404)
        {
            _logger.LogInformation($"Deployment {desiredDeployment.Name()} not found.");
            // Not an error, deployment doesn't exist yet.
        }
        catch (KubernetesException e)
        {
            _logger.LogError(e, $"Error getting Deployment {desiredDeployment.Name()}.");
            // Consider adding specific error handling (e.g., conflict resolution)
            // based on e.Status.Code or e.Status.Reason.
            return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(15)); // Requeue on error
        }

        // 3. Compare desired state with actual state and act
        try
        {
            if (existingDeployment == null)
            {
                // Deployment does not exist - Create it
                _logger.LogInformation($"Creating Deployment {desiredDeployment.Name()}.");
                await _client.CreateAsync(desiredDeployment);
                entity.Status.State = "DeploymentCreating";
                entity.Status.ObservedReplicas = 0; // Initial status
            }
            else
            {
                // Deployment exists - Check if updates are needed
                bool needsUpdate = false;
                if (existingDeployment.Spec.Replicas != desiredDeployment.Spec.Replicas)
                {
                    _logger.LogInformation($"Updating replicas for Deployment {existingDeployment.Name()} from {existingDeployment.Spec.Replicas} to {desiredDeployment.Spec.Replicas}.");
                    existingDeployment.Spec.Replicas = desiredDeployment.Spec.Replicas;
                    needsUpdate = true;
                }

                var existingImage = existingDeployment.Spec.Template.Spec.Containers.FirstOrDefault()?.Image;
                var desiredImage = desiredDeployment.Spec.Template.Spec.Containers.FirstOrDefault()?.Image;
                if (existingImage != desiredImage)
                {
                    _logger.LogInformation($"Updating image for Deployment {existingDeployment.Name()} from '{existingImage}' to '{desiredImage}'.");
                    existingDeployment.Spec.Template.Spec.Containers[0].Image = desiredImage;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    _logger.LogInformation($"Applying updates to Deployment {existingDeployment.Name()}.");
                    // Ensure the resource version matches for the update
                    desiredDeployment.Metadata.ResourceVersion = existingDeployment.Metadata.ResourceVersion;
                    await _client.UpdateObject(desiredDeployment); // Use the desired state for update
                    entity.Status.State = "DeploymentUpdating";
                }
                else
                {
                    _logger.LogInformation($"Deployment {existingDeployment.Name()} is already up-to-date.");
                }

                // Update status based on existing deployment's observed state
                entity.Status.ObservedReplicas = existingDeployment.Status?.ReadyReplicas ?? 0;
                entity.Status.State = (entity.Status.ObservedReplicas == entity.Spec.Replicas) ? "DeploymentReady" : "DeploymentProgressing";
            }
        }
        catch (KubernetesException e)
        {
            _logger.LogError(e, $"Error creating/updating Deployment for {entity.Name}.");
            entity.Status.State = "Error";
            // Requeue to retry the operation
            // Consider adding specific error handling (e.g., conflict resolution)
            return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(15)); 
        }

        // 4. Update the entity's status subresource
        entity.Status.LastUpdated = DateTime.UtcNow;
        try
        {
            // Use the UpdateStatus method to specifically update the status.
            // This avoids potential conflicts if only the status changed.
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

    public Task StatusModifiedAsync(V1Alpha1DemoEntity entity)
    {
        _logger.LogInformation($"Status updated for entity {entity.Name}.");
        // Usually, no action is needed here unless the status change itself triggers further reconciliation.
        return Task.CompletedTask;
    }

    public Task DeletedAsync(V1Alpha1DemoEntity entity)
    {
        _logger.LogInformation($"Entity {entity.Name} deleted.");
        // This method is called *after* the entity is marked for deletion 
        // *and* all registered KubeOps finalizers have completed successfully.
        // Use Finalizers for any cleanup logic that must happen *before* the
        // resource is removed from the cluster. See [Finalizers](./finalizers.md).
        return Task.CompletedTask;
    }
}
```

**Key elements:**

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

        // Example: Create or Update a Deployment based on the entity's spec
        var desiredDeployment = new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = $"{entity.Name}-deployment", // Deployment name derived from CR name
                NamespaceProperty = entity.Namespace(),
                // Set owner reference so the Deployment is garbage collected when the entity is deleted
                OwnerReferences = new List<V1OwnerReference> { entity.CreateOwnerReference() }
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = entity.Spec.Replicas, // Use replicas from CR spec
                Selector = new V1LabelSelector { MatchLabels = new Dictionary<string, string> { { "app", entity.Name } } },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta { Labels = new Dictionary<string, string> { { "app", entity.Name } } },
                    Spec = new V1PodSpec
                    {
                        Containers = new List<V1Container>
                        {
                            new V1Container
                            {
                                Name = "app-container",
                                Image = entity.Spec.Image, // Use image from CR spec
                                // Ports, env vars, volumes, etc., would go here
                            }
                        }
                    }
                }
            }
        };

        _logger.LogInformation($"Desired state: Deployment '{desiredDeployment.Name()}' with {desiredDeployment.Spec.Replicas} replicas and image '{desiredDeployment.Spec.Template.Spec.Containers[0].Image}'.");

        // 2. Get the current state of the Deployment
        V1Deployment? existingDeployment = null;
        try
        {
            existingDeployment = await _client.GetAsync<V1Deployment>(desiredDeployment.Metadata.Name, desiredDeployment.Metadata.NamespaceProperty);
        }
        catch (KubernetesException e) when (e.Status.Code == 404)
        {
            _logger.LogInformation($"Deployment {desiredDeployment.Name()} not found.");
            // Not an error, deployment doesn't exist yet.
        }
        catch (KubernetesException e)
        {
            _logger.LogError(e, $"Error getting Deployment {desiredDeployment.Name()}.");
            return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(15)); // Requeue on error
        }

        // 3. Compare desired state with actual state and act
        try
        {
            if (existingDeployment == null)
            {
                // Deployment does not exist - Create it
                _logger.LogInformation($"Creating Deployment {desiredDeployment.Name()}.");
                await _client.CreateAsync(desiredDeployment);
                entity.Status.State = "DeploymentCreating";
                entity.Status.ObservedReplicas = 0; // Initial status
            }
            else
            {
                // Deployment exists - Check if updates are needed
                bool needsUpdate = false;
                if (existingDeployment.Spec.Replicas != desiredDeployment.Spec.Replicas)
                {
                    _logger.LogInformation($"Updating replicas for Deployment {existingDeployment.Name()} from {existingDeployment.Spec.Replicas} to {desiredDeployment.Spec.Replicas}.");
                    existingDeployment.Spec.Replicas = desiredDeployment.Spec.Replicas;
                    needsUpdate = true;
                }

                var existingImage = existingDeployment.Spec.Template.Spec.Containers.FirstOrDefault()?.Image;
                var desiredImage = desiredDeployment.Spec.Template.Spec.Containers.FirstOrDefault()?.Image;
                if (existingImage != desiredImage)
                {
                    _logger.LogInformation($"Updating image for Deployment {existingDeployment.Name()} from '{existingImage}' to '{desiredImage}'.");
                    existingDeployment.Spec.Template.Spec.Containers[0].Image = desiredImage;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    _logger.LogInformation($"Applying updates to Deployment {existingDeployment.Name()}.");
                    // Ensure the resource version matches for the update
                    desiredDeployment.Metadata.ResourceVersion = existingDeployment.Metadata.ResourceVersion;
                    await _client.UpdateObject(desiredDeployment); // Use the desired state for update
                    entity.Status.State = "DeploymentUpdating";
                }
                else
                {
                    _logger.LogInformation($"Deployment {existingDeployment.Name()} is already up-to-date.");
                }

                // Update status based on existing deployment's observed state
                entity.Status.ObservedReplicas = existingDeployment.Status?.ReadyReplicas ?? 0;
                entity.Status.State = (entity.Status.ObservedReplicas == entity.Spec.Replicas) ? "DeploymentReady" : "DeploymentProgressing";
            }
        }
        catch (KubernetesException e)
        {
            _logger.LogError(e, $"Error creating/updating Deployment for {entity.Name}.");
            entity.Status.State = "Error";
            // Requeue to retry the operation
            // Consider adding specific error handling (e.g., conflict resolution)
            return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(15)); 
        }

        // 4. Update the entity's status subresource
        entity.Status.LastUpdated = DateTime.UtcNow;
        try
        {
            // Use the UpdateStatus method to specifically update the status.
            // This avoids potential conflicts if only the status changed.
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

**Important:** Always consider setting [Owner References](https://kubernetes.io/docs/concepts/overview/working-with-objects/owners-dependents/) (using the `entity.CreateOwnerReference()` helper) on resources created by your controller. This ensures Kubernetes automatically garbage collects the dependent resources (like the Deployment above) when the owner (your `V1Alpha1DemoEntity`) is deleted.

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

**Status Updates:** It's crucial to update only the `status` subresource whenever possible using `_client.UpdateStatus(entity)`. Updating the entire entity can lead to conflicts if the `spec` was modified concurrently. KubeOps automatically handles retries for status updates on conflict, but using `UpdateStatus` is the correct pattern.

 **Idempotency:** The `ReconcileAsync` method should be idempotent. This means running it multiple times with the same input `entity` should produce the same end state in the cluster. The example above achieves this by checking if the Deployment exists and comparing its spec before creating or updating.

 **Error Handling:** Implement robust error handling. Catch `KubernetesException` and potentially inspect `e.Status.Code` or `e.Status.Reason` to handle specific issues like conflicts (409), not found (404), or permissions errors (403). Use requeue results appropriately.

**RBAC Requirements:** Note that the example controller interacts with `Deployment` resources (get, create, update). Therefore, the ServiceAccount your operator runs as will need RBAC permissions for these actions on Deployments in the relevant namespaces. KubeOps helps generate this RBAC using the `[EntityRbac]` attribute (see [RBAC Generation](./rbac-generation.md)). You would typically add attributes like:

```csharp
// On the controller class or within the entity definition
[EntityRbac(typeof(V1Deployment), Verbs = RbacVerb.Get | RbacVerb.List | RbacVerb.Create | RbacVerb.Update | RbacVerb.Patch)]
```

## The `StatusModifiedAsync` Method

```csharp
    public Task StatusModifiedAsync(V1Alpha1DemoEntity entity)
    {
        _logger.LogInformation($"Status updated for entity {entity.Name}.");
        // Usually, no action is needed here unless the status change itself triggers further reconciliation.
        return Task.CompletedTask;
    }
```

This method is called when the `status` subresource of the entity is updated. It's less common to implement this method, as most logic reacts to `spec` changes handled in `ReconcileAsync`. However, if the status change itself triggers further reconciliation or if you need to react to status updates, implement this method accordingly.

## The `DeletedAsync` Method

```csharp
    public Task DeletedAsync(V1Alpha1DemoEntity entity)
    {
        _logger.LogInformation($"Entity {entity.Name} deleted.");
        // This method is called *after* the entity is marked for deletion 
        // *and* all registered KubeOps finalizers have completed successfully.
        // Use Finalizers for any cleanup logic that must happen *before* the
        // resource is removed from the cluster. See [Finalizers](./finalizers.md).
        return Task.CompletedTask;
    }
