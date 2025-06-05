# Using the KubeOps CLI

The KubeOps CLI is a .NET tool designed to assist with managing your KubeOps operator project. It provides commands for common tasks like installing Custom Resource Definitions (CRDs) into your cluster and generating deployment files.

## Installation

To use the KubeOps CLI, install it as a local .NET tool within your solution or project directory.

First, if you don't already have one, create a tool manifest file:

```bash
dotnet new tool-manifest
```

Then, install the KubeOps CLI tool:

```bash
dotnet tool install KubeOps.Cli
```

After installation, you can invoke the CLI using the `dotnet kubeops` command.

## Available Commands

The following commands are available. For detailed options and descriptions for any command, use the `-h` or `--help` flag (e.g., `dotnet kubeops install --help`).

*   **`api-version`** (Alias: `av`)
    *   Prints version information for the connected Kubernetes cluster.

*   **`install`** (Alias: `i`)
    *   Installs the [CRDs](./custom-entities.md) defined in your operator **Entities project** into the currently configured Kubernetes cluster. This command typically requires the path to your solution or Entities project file.

*   **`uninstall`** (Alias: `u`)
    *   Uninstalls the [CRDs](./custom-entities.md) defined in your operator **Entities project** from the currently configured Kubernetes cluster. Requires the path to your solution or Entities project file.

*   **`generate`** (Alias: `gen`, `g`)
    *   Generates various elements related to the operator.
    *   Common options for `generate` subcommands include `-p` or `--project` to specify the input project/assembly and `-o` or `--output-path` to specify the directory for generated YAML files.
    *   **`operator`** (Alias: `op`)
        *   Generates necessary files for deploying your operator, including manifests for Deployments, Roles, RoleBindings, and any associated webhooks. See [Deployment](./deployment.md) and [RBAC Generation](./rbac-generation.md).
    *   **`crds`**
        *   Generates Custom Resource Definition (CRD) YAML manifests based on the entity classes found in the specified **Entities project**. Essential for defining your custom resources in Kubernetes. See [Custom Entities](./custom-entities.md).
    *   **`webhooks`**
        *   Generates `ValidatingWebhookConfiguration` and `MutatingWebhookConfiguration` YAML manifests based on webhook classes and attributes found in the specified **Operator project assembly**. Requires the compiled `.dll` path. See [Webhooks](./webhooks.md).

### Example: Checking API Version

Running `dotnet kubeops api-version` connects to your cluster and displays its version details:

```bash
> dotnet kubeops api-version
   Kubernetes API Version
┌─────────────┬─────────────┐
│ Git-Version │ v1.27.2     │
│ Major       │ 1           │
│ Minor       │ 27          │
│ Platform    │ linux/arm64 │
└─────────────┴─────────────┘
```

This CLI tool simplifies several common operator development and deployment tasks.
