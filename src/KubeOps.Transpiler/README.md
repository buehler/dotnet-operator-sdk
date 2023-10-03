# KubeOps Transpiler

The KubeOps.Transpiler package provides a set of utilities
for transpiling .NET types to Kubernetes objects.

## Installation

The package is available on NuGet:

```bash
dotnet add package KubeOps.Transpiler
```

## Usage

The transpiler is used to convert .NET types to Kubernetes objects.
As an example, you can transpile valid .NET types (i.e. classes that
have a `KubernetesEntityAttribute` attached) to
`V1CustomResourceDefinition` objects:

```csharp
Crds
    .Transpile(typeof(V1TestEntity), typeof(V2TestEntity))
    .ToList();
```
