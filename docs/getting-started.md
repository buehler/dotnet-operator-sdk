# Getting Started with KubeOps

This guide will walk you through installing the necessary tools and creating your first basic Kubernetes operator using KubeOps.

## Prerequisites

Before you begin, ensure you have the following installed:

1.  **[.NET SDK](https://dotnet.microsoft.com/download):** KubeOps targets .NET 8.0 and later. You can check your version by running `dotnet --version`.
2.  **[Docker](https://www.docker.com/get-started):** Required for building container images of your operator.
3.  **[Kubernetes Cluster](https://kubernetes.io/docs/setup/):** A running Kubernetes cluster is needed to deploy and test your operator. Local options include:
    *   [minikube](https://minikube.sigs.k8s.io/docs/start/)
    *   [kind](https://kind.sigs.k8s.io/docs/user/quick-start/)
    *   [k3d](https://k3d.io/)
    *   Docker Desktop's built-in Kubernetes
4.  **[kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/):** The Kubernetes command-line tool for interacting with your cluster.

## 1. Install KubeOps CLI

The KubeOps CLI is a [.NET Tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools) that helps scaffold new operator projects and generate Kubernetes manifests.

It's recommended to install it as a local tool within your solution or project directory. This pins the tool version to your project, ensuring reproducible builds.

First, if you don't already have one, create a tool manifest file in your solution's root directory:

```bash
dotnet new tool-manifest
```

Then, install the KubeOps CLI tool:

```bash
dotnet tool install KubeOps.Cli
```

Verify the installation:

```bash
dotnet kubeops --version # You might need to be in a directory with the tool-manifest
```

## 2. Install KubeOps Templates

KubeOps provides project templates to quickly scaffold a new operator. Install them using:

```bash
dotnet new install KubeOps.Templates
```

## 3. Create Your First Operator Project

Now, use the KubeOps template to create a new operator project. Choose a directory for your project and run:

```bash
dotnet new operator -n MyFirstOperator
```

This command creates a new solution (`MyFirstOperator.sln`) using the template, typically including:

*   **`MyFirstOperator.Operator`:** The main operator project containing the `Program.cs` entry point, controllers, finalizers, and webhook handlers.
*   **`MyFirstOperator.Entities`:** A separate project to define your Custom Resource entities (CRDs).
*   **`MyFirstOperator.Test`:** A unit testing project.

## 4. Understand the Project Structure

*   **`Entities/V1DemoEntity.cs`:** An example Custom Resource definition.
*   **`Operator/Controller/V1DemoEntityController.cs`:** An example controller that watches for changes to `V1DemoEntity` resources.
*   **`Operator/Program.cs`:** The main entry point that configures and runs the operator using the `OperatorBuilder`.

## 5. Run the Operator Locally

Navigate into the operator project directory:

```bash
cd MyFirstOperator/MyFirstOperator.Operator
```

Run the operator using the .NET CLI:

```bash
dotnet run
```

The operator will start, connect to your currently configured Kubernetes cluster (check `kubectl config current-context`), install the necessary CRD (`V1DemoEntity`), and begin watching for resources.

Congratulations! You've created and run your first basic KubeOps operator.

Next Steps:

*   Learn more about defining [Custom Entities](./custom-entities.md).
*   Dive deeper into writing [Controller Logic](./controllers.md).

This provides a basic overview. Subsequent sections will dive deeper into defining entities, implementing controllers, and handling finalizers.

## Full Example

For a complete, runnable example demonstrating these concepts, refer to the main example operator project:
[`examples/Operator`](../examples/Operator)
