# KubeOps.Abstractions

[![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Abstractions?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Abstractions/absoluteLatest)

This package provides the fundamental building blocks for the KubeOps SDK. It defines the core interfaces, abstract base classes, and [.NET attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/) used throughout the operator framework.

Think of this package as the contract definition for key KubeOps components.

## Key Components

The primary abstractions defined here include:

*   **Entities:**
    *   `CustomKubernetesEntity<TSpec, TStatus>`: The base class for defining your [Custom Resources](https://kubernetes.io/docs/concepts/extend-kubernetes/api-extension/custom-resources/).
    *   `[KubernetesEntity]`: Attribute to mark a C# class as a Kubernetes entity and define its Group, Version, and Kind (GVK).
    *   `[EntityScope]`: Attribute to define whether an entity is `Namespaced` or `Cluster`-scoped.
    *   Various validation attributes (`[Description]`, `[RangeMaximum]`, `[Pattern]`, etc.) used to generate OpenAPI validation rules for the CRD. See [.NET Data Annotations](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations?view=net-8.0) for common attributes.
    *   `[EntityRbac]`: Attribute applied to Controllers and Webhooks to declare the RBAC permissions required by the operator for a specific entity type. Processed by the CLI/Generators.
    *   See: [Defining Custom Entities](../../docs/custom-entities.md)
*   **Controllers:**
    *   `IResourceController<TEntity>`: The interface that must be implemented by controllers to handle the reconciliation logic for a specific entity type.
    *   See: [Implementing Controllers](../../docs/controllers.md)
*   **Finalizers:**
    *   `IResourceFinalizer<TEntity>`: The interface for implementing cleanup logic that runs *before* an entity is deleted.
    *   `[ResourceFinalizerMetadata]`: Attribute to associate a unique identifier with a finalizer implementation.
    *   See: [Using Finalizers](../../docs/finalizers.md)
*   **Webhooks:**
    *   `IAdmissionWebhook<TEntity, TOperation>`: Base interface for admission webhooks.
    *   `IMutationWebhook<TEntity, TOperation>`: Interface for webhooks that can modify entities during admission.
    *   `IValidationWebhook<TEntity, TOperation>`: Interface for webhooks that validate entities during admission.
    *   See: [Admission Webhooks](../../docs/webhooks.md)
*   **Common Utilities:** Various helper methods and base types used internally by the SDK.

## When to Use

Most projects building a KubeOps operator will reference the main `KubeOps.Operator` package, which includes this abstractions package as a dependency.

By depending only on this package, you can define your entities and interfaces without pulling in the full operator runtime or Kubernetes client logic, promoting better separation of concerns. This is primarily useful if you want to:

*   Define your CRD entity classes in a separate library, shared between your operator and potentially other applications.
*   Build tools that need to understand KubeOps entity definitions without needing the operator runtime.
