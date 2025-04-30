# Watching Resources

In the context of Kubernetes operators, "watching" refers to the process of monitoring changes to specific resources within the cluster. When a change occurs (creation, update, deletion), the operator can react accordingly, often by triggering reconciliation logic.

## Watching the Controller's Primary Entity

KubeOps simplifies the most common use case: watching the primary resource type that a controller is designed to manage.

When you register a controller using `IOperatorBuilder.AddController<TImplementation, TEntity>()`, KubeOps automatically sets up a watcher for the specified `TEntity` type. Any changes to resources of this type will trigger the appropriate methods on your controller instance (e.g., `ReconcileAsync`, `StatusModifiedAsync`, `DeletedAsync`).

You **do not** need to explicitly configure a watcher for the controller's primary entity; this is handled internally by the framework.

For more details on controller methods, see the [Controllers](./controllers.md) documentation.

## Handling Related Resources

While KubeOps excels at watching the controller's main entity, it **does not currently provide a direct `IOperatorBuilder` method** to configure a controller to *also* automatically watch arbitrary *related* resources and trigger reconciliation based on *their* changes.

For example, if your `MyDatabase` controller needs to react when a related `Secret` containing credentials changes, you need to handle this within your controller's logic.

Common patterns for dealing with related resources include:

1.  **Fetching within `ReconcileAsync`**: The most common approach is to use the injected `IKubernetesClient` within your `ReconcileAsync` method to fetch the current state of any related resources needed for reconciliation. You compare the fetched state to the desired state and make adjustments as necessary.

    ```csharp
    using k8s;
    // ... other usings

    public class MyDatabaseController : IResourceController<V1MyDatabase>
    {
        private readonly IKubernetesClient _client;

        public MyDatabaseController(IKubernetesClient client)
        {
            _client = client;
        }

        public async Task<ResourceControllerResult?> ReconcileAsync(V1MyDatabase entity)
        {
            // Fetch the related secret defined in the spec
            var secretName = entity.Spec.CredentialsSecretName;
            var secret = await _client.GetAsync<V1Secret>(secretName, entity.Metadata.NamespaceProperty);

            if (secret == null)
            {
                // Handle missing secret - perhaps create it or requeue
                return ResourceControllerResult.RequeueEvent(TimeSpan.FromMinutes(1));
            }

            // ... use secret data to reconcile the database ...

            return null; // Reconciliation successful
        }

        // Other controller methods...
    }
    ```

2.  **Using Owner References**: When your controller *creates* dependent resources (like a `Deployment` or `Service` for your `MyDatabase`), setting [Owner References](https://kubernetes.io/docs/concepts/overview/working-with-objects/owners-dependents/) is crucial. This links the dependent resource to your custom resource. If your custom resource is deleted, Kubernetes automatically garbage-collects (deletes) the dependent resources. This simplifies cleanup but doesn't trigger reconciliation on *changes* to the dependent resource.

    KubeOps provides helper methods for creating owner references. See the [example in the Controllers documentation](./controllers.md#using-the-kubernetes-client) where `entity.CreateOwnerReference()` is used when creating a dependent ConfigMap.

If direct watching of related resources (i.e., triggering reconciliation of `MyDatabase` when its `Secret` changes) is a feature you need, consider discussing it or contributing to the KubeOps project.
