# KubeOps Kubernetes Client

[![Nuget](https://img.shields.io/nuget/vpre/KubeOps.KubernetesClient?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.KubernetesClient/absoluteLatest)

This package provides an **enhanced, developer-friendly interface** for interacting with the Kubernetes API, built on top of the official [kubernetes-client/csharp](https://github.com/kubernetes-client/csharp) library. While the official client is powerful, it often requires verbose calls, especially for Custom Resources.

The `KubeOps.KubernetesClient` aims to simplify common operator tasks by offering:

*   **True Generic Methods:** Perform operations like `Get`, `List`, `Create`, `Update`, `Delete`, and `Watch` on *any* Kubernetes resource type (including your custom resources defined with `[KubernetesEntity]`) using strongly-typed generic methods, without manually providing API group, version, and plural name.
*   **Simplified API:** Reduces the boilerplate needed for common CRUD operations.
*   **Type Safety:** Leverages C# generics for better compile-time checking.

This is an "enhanced" version of the original
[Google Kubernetes Client](https://github.com/kubernetes-client/csharp).
It extends the original client with some additional features, like
true generics and method variants. The original `GenericClient` does support
"generics", but only in a limited way. The client needs to be initialized
with group and kind information as well.

It acts as a wrapper, handling the complexities of determining the correct API endpoint and resource mapping based on the provided C# type.

## Usage

### Dependency Injection (Recommended)

When using the main `KubeOps.Operator` package, an instance of `IKubernetesClient` is automatically registered in the .NET Dependency Injection container. You can simply inject it into your controllers, finalizers, or webhooks:

```csharp
using KubeOps.KubernetesClient;
using KubeOps.Operator.Controller;
using MyOperator.Entities; // Your custom entity
using k8s.Models; // For built-in types like V1Pod

public class MyResourceController : IResourceController<V1MyResource>
{
    private readonly IKubernetesClient _client;

    public MyResourceController(IKubernetesClient client)
    {
        _client = client;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(V1MyResource entity)
    {
        // Use the client to interact with the cluster
        var pod = await _client.GetAsync<V1Pod>("my-pod", entity.Namespace());
        if (pod == null)
        {
            var newPod = new V1Pod { /* ... */ };
            await _client.CreateAsync(newPod);
        }

        // Get a custom resource
        var otherResource = await _client.GetAsync<V1OtherResource>("other-resource-name", entity.Namespace());

        return null; // Requeue later
    }

    // ... other methods
}
```

### Standalone Usage

If you need to use the client outside the main KubeOps operator framework (e.g., in a command-line tool or script), you can instantiate it directly. It will automatically attempt to load configuration based on standard Kubernetes conventions ([Kubeconfig file](https://kubernetes.io/docs/concepts/configuration/organize-cluster-access-kubeconfig/) or [in-cluster service account](https://kubernetes.io/docs/tasks/configure-pod-container/configure-service-account/)).

```csharp
using KubeOps.KubernetesClient;
using k8s.Models;

// Instantiate the client
IKubernetesClient client = new KubernetesClient();

// List all namespaces
var namespaces = await client.ListAsync<V1Namespace>();
foreach (var ns in namespaces)
{
    Console.WriteLine($"Namespace: {ns.Name()}");
}

// Get a specific ConfigMap
var configMap = await client.GetAsync<V1ConfigMap>("my-config", "default");
if (configMap != null)
{
    Console.WriteLine($"ConfigMap Data: {string.Join(',', configMap.Data)}");
}

```

For advanced configuration (e.g., custom Kubeconfig paths, timeouts), refer to the underlying `k8s.KubernetesClientConfiguration` documentation from the official client library.

## Examples

An example of the client that lists all namespaces in the cluster:

```csharp
var client = new KubernetesClient() as IKubernetesClient;

// Get all namespaces in the cluster.
var namespaces = await client.ListAsync<V1Namespace>();
```

**Get a specific resource:**

```csharp
// Get a Pod in the 'production' namespace
var pod = await client.GetAsync<V1Pod>("my-app-pod-xyz", "production");

// Get a custom resource (assuming V1MyCrd is defined)
var myCrd = await client.GetAsync<V1MyCrd>("my-instance", "default");
```

**List resources (with optional namespace):**

```csharp
// List all pods in the 'staging' namespace
var podsInStaging = await client.ListAsync<V1Pod>("staging");

// List all custom resources of type V1MyCrd across all namespaces (for cluster-scoped or if allowed by RBAC)
var allMyCrds = await client.ListAsync<V1MyCrd>(null);
```

**Create a resource:**

```csharp
var newConfigMap = new V1ConfigMap
{
    Metadata = new V1ObjectMeta { Name = "new-map", NamespaceProperty = "default" },
    Data = new Dictionary<string, string> { { "key", "value" } }
};
var createdMap = await client.CreateAsync(newConfigMap);
```

**Update a resource:**

```csharp
var existingPod = await client.GetAsync<V1Pod>("my-pod", "default");
if (existingPod != null)
{
    existingPod.Metadata.Annotations ??= new Dictionary<string, string>();
    existingPod.Metadata.Annotations["my-annotation"] = "updated-value";
    var updatedPod = await client.UpdateAsync(existingPod);
}
```

**Delete a resource:**

```csharp
await client.DeleteAsync<V1Pod>("pod-to-delete", "default");

var crdToDelete = await client.GetAsync<V1MyCrd>("crd-instance-to-delete", "dev");
if (crdToDelete != null)
{
    await client.DeleteAsync(crdToDelete);
}
