# KubeOps Transpiler

The `KubeOps.Transpiler` package provides utilities primarily focused on **generating Kubernetes Custom Resource Definition (CRD) manifests (YAML/JSON) from .NET type definitions**.

It allows you to define your custom resources using C# classes and attributes, and then automatically create the corresponding Kubernetes CRD schema required to register your resource type with the cluster.

## Installation

The package is available on NuGet:

```bash
dotnet add package KubeOps.Transpiler
```

## Usage

The core functionality revolves around inspecting .NET assemblies and their types to find entities marked for Kubernetes and converting their structure into a CRD format.

### Generating CRDs

You can transpile .NET types decorated with the `[KubernetesEntity]` attribute (typically defined in your operator project) into `V1CustomResourceDefinition` objects from the `k8s-operator` library.

This process often utilizes `System.Reflection.MetadataLoadContext` to inspect assemblies without fully loading or executing them, which is useful in build-time tools or CLIs.

**Example:**

```csharp
using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Transpiler;
using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// Define your custom resource class (usually in a separate project)
[KubernetesEntity(Group = "ewassef.dev", ApiVersion = "v1alpha1", Kind = "MyResource")]
public class MyCustomResource : CustomKubernetesEntity
{
    public MyCustomResourceSpec Spec { get; set; } = new();
}

public class MyCustomResourceSpec
{
    public string? Message { get; set; }
    public int Replicas { get; set; }
}

// --- Transpilation Logic (e.g., in a build task or utility) ---

// 1. Get the assembly containing your custom resource types
//    (Adjust path as needed or use Assembly.LoadFrom/Assembly.Load)
var assemblyPath = "path/to/your/Operator.Project.dll";
var assembly = Assembly.LoadFrom(assemblyPath);

// 2. Create a MetadataLoadContext
//    Provide assembly resolver paths (e.g., NuGet package directories)
var resolver = new PathAssemblyResolver(Directory.GetFiles(Path.GetDirectoryName(assemblyPath)!, "*.dll"));
using var mlc = new MetadataLoadContext(resolver);
var assemblyInMlc = mlc.LoadFromAssemblyPath(assemblyPath);

// 3. Transpile types from the assembly
var crds = assemblyInMlc.GetCustomResourceDefinitions(); // KubeOps.Transpiler extension method

// 4. (Optional) Serialize to YAML
var serializer = new SerializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance) // Common for Kubernetes YAML
    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults) // Reduce YAML size
    .Build();

foreach (var crd in crds)
{
    var crdYaml = serializer.Serialize(crd);
    Console.WriteLine("---"); // YAML document separator
    Console.WriteLine(crdYaml);
    // Or write to a file, e.g., File.WriteAllText($"{crd.Metadata.Name}.crd.yaml", crdYaml);
}
```

### Use Cases

*   **KubeOps CLI:** This package is the engine behind the `dotnet kubeops generate crd` command.
*   **Custom Build Tasks:** Integrate CRD generation directly into your MSBuild process.
*   **Schema Validation Tools:** Use the generated CRD schema for validating custom resource YAML files.

For more details on defining the C# classes themselves, see the main KubeOps documentation on [Custom Entities](../../docs/custom-entities.md).
