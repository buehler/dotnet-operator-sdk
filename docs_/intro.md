# Introduction to KubeOps

Welcome to KubeOps, the SDK for building [Kubernetes Operators](https://kubernetes.io/docs/concepts/extend-kubernetes/operator/) with [.NET](https://dotnet.microsoft.com/)!

## What are Kubernetes Operators?

In Kubernetes, Operators are software extensions that use [Custom Resources](https://kubernetes.io/docs/concepts/extend-kubernetes/api-extension/custom-resources/) (CRs) to manage applications and their components. The Operator pattern aims to capture the knowledge of human operational tasks, automating tasks beyond what Kubernetes provides out-of-the-box. Think of them as runtime controllers specifically tailored for your application's lifecycle.

## Why KubeOps?

While the official Go-based [Operator SDK](https://sdk.operatorframework.io/) and [Kubebuilder](https://book.kubebuilder.io/) are powerful tools, KubeOps offers several advantages for developers in the .NET ecosystem:

*   **Leverage Existing Skills:** Write operators using familiar C#, .NET libraries, and development tools (like Visual Studio or VS Code).
*   **Strong Typing:** Define your Custom Resource Definitions (CRDs) using C# classes, benefiting from compile-time checks and IntelliSense.
*   **Integration:** Seamlessly integrates with ASP.NET Core for features like [Admission Webhooks](https://kubernetes.io/docs/reference/access-authn-authz/extensible-admission-controllers/).
*   **Simplified Development:** Abstracts away much of the boilerplate involved in interacting with the Kubernetes API, handling events, and managing resources.

## Core Concepts in KubeOps

KubeOps is built around a few key concepts:

*   **[Entities](./custom-entities.md):** Plain C# classes decorated with attributes that define your Custom Resources (CRs) and their corresponding Custom Resource Definitions (CRDs).
*   **[Controllers](./controllers.md):** Classes that contain the reconciliation logic. They watch for changes to your custom resources (and potentially other Kubernetes resources) and take actions to drive the cluster state towards the desired state defined in the CR.
*   **[Finalizers](./finalizers.md):** Mechanisms to run cleanup logic *before* a custom resource managed by your operator is actually deleted from the cluster.
*   **[Webhooks](./webhooks.md):** (Optional) HTTP callbacks (implemented using ASP.NET Core) that Kubernetes calls for validating, mutating, or converting resources during API operations.

## KubeOps SDK Structure

The KubeOps SDK is composed of several NuGet packages:

*   **`KubeOps.Operator`:** The core runtime engine that handles watching resources, the reconciliation loop, event dispatching, and leader election.
*   **`KubeOps.KubernetesClient`:** An enhanced, developer-friendly client for interacting with the Kubernetes API.
*   **`KubeOps.Abstractions`:** Defines the core interfaces, attributes, and base classes used across the SDK.
*   **`KubeOps.Transpiler`:** Contains the logic for transpiling C# entity definitions into Kubernetes Custom Resource Definition (CRD) YAML specifications.
*   **`KubeOps.Generator`:** Provides Roslyn source generators that create extension methods (e.g., for `IServiceCollection`) to simplify the registration of operator components (controllers, finalizers, webhooks) in your `Program.cs`.
*   **`KubeOps.Cli`:** A .NET global tool for scaffolding new operator projects and generating Kubernetes manifests (CRDs, RBAC roles/bindings, Operator Deployments, Webhook configurations) based on your code.
*   **`KubeOps.Operator.Web`:** Integrates KubeOps with ASP.NET Core for hosting webhook endpoints.

Ready to build your first operator? Head over to the [**Getting Started**](./getting-started.md) guide!
