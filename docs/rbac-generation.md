# RBAC Generation

Kubernetes uses [Role-Based Access Control (RBAC)](https://kubernetes.io/docs/reference/access-authn-authz/rbac/) to regulate access to resources within a cluster. Operators, by their nature, need specific permissions to watch, get, list, create, update, and delete the resources they manage (both custom resources and built-in ones like Pods, ConfigMaps, etc.).

Defining these RBAC rules manually can be tedious and error-prone. KubeOps provides mechanisms to automatically generate the necessary `Role` or `ClusterRole` manifests based on attributes declared in your code.

## Declaring Permissions

KubeOps allows you to declare the permissions required by your operator components (Controllers, Finalizers, Webhooks) directly in your C# code using the `[EntityRbac]` attribute.

This attribute can be applied to your controller, finalizer, or webhook classes.

```csharp
using KubeOps.Operator.Attributes;
using KubeOps.Operator.Controller;
using k8s.Models;

// Example: Controller managing DemoEntity and needing access to ConfigMaps
[EntityRbac(typeof(V1Alpha1DemoEntity), Verbs = RbacVerb.All)] // Permissions for the primary entity
[EntityRbac(typeof(V1ConfigMap), Verbs = RbacVerb.Get | RbacVerb.List | RbacVerb.Create | RbacVerb.Update | RbacVerb.Patch | RbacVerb.Watch)] // Permissions for ConfigMaps
public class DemoController : IResourceController<V1Alpha1DemoEntity>
{
    // ... controller logic ...

    public Task<ResourceControllerResult?> ReconcileAsync(V1Alpha1DemoEntity entity)
    {
        // Logic here might involve getting, creating, or updating ConfigMaps
        // The [EntityRbac] attributes ensure the operator's ServiceAccount
        // will have the necessary permissions generated for it.
        throw new NotImplementedException();
    }

    public Task StatusModifiedAsync(V1Alpha1DemoEntity entity) => throw new NotImplementedException();
    public Task DeletedAsync(V1Alpha1DemoEntity entity) => throw new NotImplementedException();
}
```

*   **Attribute Target:** Apply `[EntityRbac]` to the class definition of your controller, finalizer, or webhook.
*   **`typeof(TEntity)`:** Specifies the Kubernetes resource type the permission applies to (e.g., `typeof(V1ConfigMap)`, `typeof(V1Alpha1DemoEntity)`).
*   **`Verbs`:** A flags enum (`KubeOps.Operator.Attributes.RbacVerb`) specifying the actions allowed. Common verbs include `Get`, `List`, `Watch`, `Create`, `Update`, `Patch`, `Delete`. `RbacVerb.All` grants all standard permissions.

## Generating Manifests

Once you have decorated your components with the necessary `[EntityRbac]` attributes, you can use the KubeOps CLI to generate the corresponding RBAC manifests.

The command `dotnet kubeops generate operator` is responsible for this:

```bash
# Generate deployment manifests (including RBAC) for MyOperator.csproj into './deploy'
dotnet kubeops generate operator -s ./MyOperator.csproj -o ./deploy
```

**How it Works:**

1.  The CLI tool scans the specified project (`-s`) for classes implementing KubeOps interfaces (controllers, etc.).
2.  It uses the logic from the [`KubeOps.Transpiler`](../../src/KubeOps.Transpiler/README.md) package to find all `[EntityRbac]` attributes on these classes.
3.  It aggregates these permissions.
4.  Based on the operator's configured scope (usually inferred from settings passed to the `OperatorBuilder` in `Program.cs`, like watching specific namespaces or all namespaces), it generates either:
    *   A `Role` and `RoleBinding` manifest (for namespace scope).
    *   A `ClusterRole` and `ClusterRoleBinding` manifest (for cluster scope).
5.  These generated YAML files are written to the specified output directory (`-o`), alongside other deployment manifests like the `Deployment` and `ServiceAccount`.

By using the `[EntityRbac]` attribute and the `generate operator` command, you can ensure your operator's `ServiceAccount` is granted the precise permissions it needs to function correctly, significantly simplifying RBAC management.

For more details on the CLI command, see the [CLI Usage Documentation](./cli.md).
