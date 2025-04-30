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
    *   Installs the [CRDs](./custom-entities.md) defined in your operator project into the currently configured Kubernetes cluster. This command typically requires the path to your solution or project file.

*   **`uninstall`** (Alias: `u`)
    *   Uninstalls the [CRDs](./custom-entities.md) defined in your operator project from the currently configured Kubernetes cluster. Requires the path to your solution or project file.

*   **`generate`** (Alias: `gen`, `g`)
    *   Generates various elements related to the operator.
    *   **`operator`** (Alias: `op`)
        *   Generates necessary files for deploying your operator, including manifests for Deployments, Roles, RoleBindings, and any associated webhooks.

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
