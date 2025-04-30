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

*   **Entities:** Plain C# classes decorated with attributes that define your Custom Resources (CRs) and their corresponding CRDs.
*   **Controllers:** Classes that contain the reconciliation logic. They watch for changes to your custom resources (and potentially other Kubernetes resources) and take actions to drive the cluster state towards the desired state defined in the CR.
*   **Finalizers:** Mechanisms to run cleanup logic before a custom resource managed by your operator is deleted from the cluster.
*   **Webhooks:** (Optional) HTTP callbacks (implemented using ASP.NET Core) that Kubernetes calls for validation or mutation of resources during create, update, or delete operations.

Ready to build your first operator? Head over to the **Getting Started** guide!
