# KubeOps

**Build Kubernetes Operators in .NET with Ease**

[![.NET Pre-Release](https://github.com/buehler/dotnet-operator-sdk/actions/workflows/dotnet-release.yml/badge.svg?branch=main)](https://github.com/buehler/dotnet-operator-sdk/actions/workflows/dotnet-release.yml)
[![.NET Release](https://github.com/buehler/dotnet-operator-sdk/actions/workflows/dotnet-release.yml/badge.svg?branch=release)](https://github.com/buehler/dotnet-operator-sdk/actions/workflows/dotnet-release.yml)
[![Scheduled Code Security Testing](https://github.com/buehler/dotnet-operator-sdk/actions/workflows/security-analysis.yml/badge.svg?event=schedule)](https://github.com/buehler/dotnet-operator-sdk/actions/workflows/security-analysis.yml)

**KubeOps** is a [Kubernetes Operator](https://kubernetes.io/docs/concepts/extend-kubernetes/operator/) SDK designed for [.NET](https://dotnet.microsoft.com/) developers. It allows you to leverage your C# skills and the rich .NET ecosystem to build powerful Kubernetes controllers that automate the management of complex applications. KubeOps simplifies operator development by providing high-level abstractions, code generators, and helper utilities.

**For comprehensive documentation, tutorials, and API references, please visit the official [KubeOps Documentation Site](https://buehler.github.io/dotnet-operator-sdk/).**

The documentation is also provided within the code itself (description of methods and classes), and each package contains a README with further information.

## Key Features

*   **Define CRDs in C#:** Model your [Custom Resource Definitions (CRDs)](https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definitions/) using plain C# classes and attributes.
*   **Controller Logic:** Implement reconciliation logic using the `IResourceController<TEntity>` interface.
*   **Finalizers:** Easily add cleanup logic before resource deletion with `IResourceFinalizer<TEntity>`.
*   **Webhooks:** Create [Admission](https://kubernetes.io/docs/reference/access-authn-authz/extensible-admission-controllers/) (validating/mutating) and [Conversion](https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definition-versioning/#webhook-conversion) webhooks integrated with ASP.NET Core.
*   **Code Generation:** Includes Roslyn source generators and a CLI tool (`dotnet kubeops`) to automate boilerplate code for CRDs, controllers, and RBAC rules.
*   **Enhanced Kubernetes Client:** Provides convenience methods built on top of the official client library.
*   **Leader Election:** Automatic handling for high-availability operator deployments.
*   **Testing Support:** Provides utilities and patterns to help with unit and integration testing.

## Packages

All packages target [.NET 8.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/overview) and [.NET 9.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview), leveraging modern C# features. The underlying Kubernetes client library (`KubernetesClient.Official`) also follows this versioning strategy.

The SDK is designed to be modular. You can include only the packages you need:

| Package                                                              | Description                                                                                                                                                             | Latest Version                                                                                                                                                          |
|----------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [KubeOps.Abstractions](./src/KubeOps.Abstractions/README.md)         | Defines core interfaces, attributes (like `[KubernetesEntity]`), and base classes used across the SDK. Essential for defining your custom resources and controllers.       | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Abstractions?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Abstractions/absoluteLatest)         |
| [KubeOps.Cli](./src/KubeOps.Cli/README.md)                           | A [.NET Tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools) providing commands for scaffolding projects, generating [Custom Resource Definitions (CRDs)](https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definitions/), and more. | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Cli?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Cli/absoluteLatest)                           |
| [KubeOps.Generator](./src/KubeOps.Generator/README.md)               | Contains [Roslyn Source Generators](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) to automate boilerplate code generation for CRDs and controllers based on your definitions. | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Generator?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Generator/absoluteLatest)               |
| [KubeOps.KubernetesClient](./src/KubeOps.KubernetesClient/README.md) | Provides an enhanced client for interacting with the [Kubernetes API](https://kubernetes.io/docs/reference/kubernetes-api/), built on top of the official `KubernetesClient` library. Offers convenience methods for common operator tasks. | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.KubernetesClient?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.KubernetesClient/absoluteLatest) |
| [KubeOps.Operator](./src/KubeOps.Operator/README.md)                 | The main engine of the SDK. Handles reconciling resources, watching for changes, and managing the operator lifecycle. This is the primary package needed to run an operator. | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Operator?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Operator/absoluteLatest)                 |
| [KubeOps.Operator.Web](./src/KubeOps.Operator.Web/README.md)         | Integrates the operator with ASP.NET Core to expose endpoints for features like [Admission Webhooks](https://kubernetes.io/docs/reference/access-authn-authz/extensible-admission-controllers/). | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Operator.Web?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Operator.Web/absoluteLatest)         |
| [KubeOps.Transpiler](./src/KubeOps.Transpiler/README.md)             | Utilities for converting .NET type definitions into Kubernetes YAML manifests, specifically for [CRDs](https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definitions/) and [RBAC](https://kubernetes.io/docs/reference/access-authn-authz/rbac/) rules. | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Transpiler?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Transpiler/absoluteLatest)             |

*Note: NuGet badges show the latest pre-release version.*

## Contribution

If you want to contribute, feel free to open a pull request or write issues :-)
Read more about contribution (especially for setting up your local environment)
in the [CONTRIBUTING file](./CONTRIBUTING.md).

In short:

- Check out the code
- Develop on KubeOps
- Use some Kubernetes to run the test operator against
- Create tests
- Build the whole solution (lint warnings will result in an error)
- Open PR

## Motivation

KubeOps aims to provide a first-class experience for developing Kubernetes operators within the .NET ecosystem, offering an alternative to Go-based SDKs like Kubebuilder and Operator SDK, while embracing familiar C# patterns and tooling.
