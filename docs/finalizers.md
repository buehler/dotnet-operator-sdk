# Using Finalizers

When a user requests to delete a Kubernetes object, the API server doesn't immediately remove it. Instead, it sets a `metadata.deletionTimestamp` on the object. The object remains visible via the API until all specified [**Finalizers**](https://kubernetes.io/docs/concepts/overview/working-with-objects/finalizers/) are removed from its `metadata.finalizers` list.

Finalizers run *before* the object is actually removed, giving controllers a chance to perform cleanup. This is different from the controller's `DeletedAsync` method, which runs *after* finalizers are complete and the object is gone.

Finalizers are crucial for operators because they provide a hook to perform cleanup actions *before* a custom resource is actually deleted from the cluster. This is essential for tasks like:

*   Deleting external resources managed by the operator (e.g., cloud databases, DNS records).
*   Performing ordered cleanup of dependent Kubernetes resources.
*   Ensuring graceful shutdown procedures are followed.

Remember that the `DeletedAsync` method on the controller is called *after* finalizers have run and the object is truly being removed from the API server. Therefore, `FinalizeAsync` is the correct place for pre-deletion cleanup logic that needs to interact with the resource or dependent resources before they disappear.

## KubeOps Finalizer Management

KubeOps simplifies finalizer usage. When you register an implementation of the `IResourceFinalizer<TEntity>` interface for your custom entity type (`TEntity`), KubeOps automatically:

1.  **Adds** a specific finalizer (e.g., `operator.kubeops.dev/myentityfinalizer`) to the `metadata.finalizers` list of any newly created or reconciled `TEntity` resource.
2.  **Detects** when a user requests deletion of a `TEntity` resource (by observing the `metadata.deletionTimestamp`).
3.  **Calls** the `FinalizeAsync` method of your registered `IResourceFinalizer<TEntity>` implementation.
4.  **Removes** its finalizer from the `metadata.finalizers` list (which is a list of strings, allowing multiple finalizers) *only after* your `FinalizeAsync` method completes successfully.

Once all finalizers (including the one managed by KubeOps) are removed, the Kubernetes garbage collector physically deletes the resource.

## Implementing `IResourceFinalizer<TEntity>`

To implement cleanup logic, create a class that implements `IResourceFinalizer<TEntity>` from the `KubeOps.Operator.Finalizer` namespace.

```csharp
using KubeOps.KubernetesClient;
using KubeOps.Operator.Finalizer;
using k8s.Models; // For V1ConfigMap
using Microsoft.Extensions.Logging;
using MyFirstOperator.Entities; // Your entity namespace

// The finalizer identifier is used in the metadata.finalizers list.
// Keep it descriptive and unique, typically related to the finalizer's purpose.
[ResourceFinalizerMetadata("operator.kubeops.dev/demofinalizer")] 
public class V1Alpha1DemoEntityFinalizer : IResourceFinalizer<V1Alpha1DemoEntity>
{
    private readonly ILogger<V1Alpha1DemoEntityFinalizer> _logger;
    private readonly IKubernetesClient _client; // Inject client if needed for cleanup

    public V1Alpha1DemoEntityFinalizer(ILogger<V1Alpha1DemoEntityFinalizer> logger, IKubernetesClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task FinalizeAsync(V1Alpha1DemoEntity entity)
    {
        _logger.LogInformation($"Finalizing entity {entity.Name} in namespace {entity.Namespace()}.");

        // --- Your Cleanup Logic Goes Here ---
        // Example: Delete the ConfigMap created by the controller
        var configMapName = $"{entity.Name}-config";
        try
        {
            await _client.DeleteObject<V1ConfigMap>(configMapName, entity.Namespace());
            _logger.LogInformation($"Deleted associated ConfigMap {configMapName} for {entity.Name}.");
        }
        catch (k8s.Autorest.HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning($"ConfigMap {configMapName} already deleted for {entity.Name}.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error deleting ConfigMap {configMapName} during finalization for {entity.Name}.");
            // If cleanup fails, throwing an exception here will prevent
            // KubeOps from removing the finalizer, thus blocking deletion.
            // Handle transient errors carefully, perhaps with internal retries.
            throw; 
        }
        // --------------------------------------

        _logger.LogInformation($"Finalization complete for {entity.Name}.");
        // No return value needed. Successful completion allows KubeOps to remove the finalizer.
    }
}
```

Key Points:

*   **`[ResourceFinalizerMetadata("...")]`**: **(Required)** This attribute assigns a unique identifier string for your finalizer. KubeOps uses this string when adding/removing the finalizer from the resource's metadata. Use a DNS-like name convention.
*   **`TEntity`**: The specific custom resource type this finalizer applies to.
*   **`FinalizeAsync(TEntity entity)`**: This method contains your cleanup logic. It's called when the resource has a `deletionTimestamp` and your finalizer is present in `metadata.finalizers`.
*   **Idempotency:** Your cleanup logic should be idempotent, meaning running it multiple times should have the same effect as running it once. Kubernetes might call the finalizer multiple times if previous attempts failed or the operator restarted.
*   **Error Handling:** If `FinalizeAsync` throws an exception, KubeOps will *not* remove its finalizer. This effectively blocks the deletion of the resource until the finalizer succeeds. Implement robust error handling and potentially internal retries for transient issues.
*   **Return Value:** The `FinalizeAsync` method returns `Task`. Successful completion (i.e., the Task finishes without throwing an exception) signals to KubeOps that the finalization logic is complete and the finalizer can be removed.
*   **Registration:** Like controllers, finalizers must be registered during operator startup (usually in `Program.cs` or a dependency injection setup method). KubeOps uses the `IOperatorBuilder` for this:

    ```csharp
    // In your Program.cs or startup configuration
    builder.Services.AddKubernetesOperator(options => { /* configure options if needed */ })
        .AddController<V1Alpha1DemoEntityController, V1Alpha1DemoEntity>()
        // Register the finalizer implementation for the specific entity
        // The identifier string MUST match the one in the [ResourceFinalizerMetadata] attribute.
        .AddFinalizer<V1Alpha1DemoEntityFinalizer, V1Alpha1DemoEntity>("operator.kubeops.dev/demofinalizer");
    ```

By using finalizers correctly, you ensure that your operator cleans up after itself, maintaining a healthy state in the cluster and managing any external resources appropriately.

## Example

See a practical example of a finalizer implementation in the GitHub repository:
[`examples/Operator/Finalizer/`](https://github.com/ewassef/dotnet-operator-sdk/tree/main/examples/Operator/Finalizer/)
