# KubeOps Kubernetes Client

This is an "enhanced" version of the original
[Google Kubernetes Client](https://github.com/kubernetes-client/csharp).
It extends the original client with some additional features, like
true generics and method variants. The original `GenericClient` does support
"generics", but only in a limited way. The client needs to be initialized
with group and kind information as well.

## Usage

An example of the client that lists all namespaces in the cluster:

```csharp
// Creates the client with the default config.
var client = new KubernetesClient();

// Get all namespaces in the cluster.
var namespaces = await client.List<V1Namespace>();
```
