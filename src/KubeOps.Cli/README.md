# KubeOps CLI

The KubeOps CLI (`dotnet kubeops`) is a [.NET Tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) designed to streamline common development tasks when building Kubernetes operators with KubeOps. It helps with generating Custom Resource Definition (CRD) manifests, installing/uninstalling those CRDs in a cluster, generating deployment manifests, and inspecting your cluster.

## Installation

To install the CLI into your solution / project, first create a new tool manifest:

```bash
dotnet new tool-manifest
```

Then install the CLI:

```bash
dotnet tool install KubeOps.Cli
```

This installs the tool locally for the current repository. You can then invoke it using `dotnet kubeops <command>`.

## Available Commands

Below is an overview of the available commands. For detailed options and descriptions for any command, use the `-h` or `--help` flag (e.g., `dotnet kubeops generate --help`).

### Cluster Interaction
*   **`api-version` (alias `av`)**: Displays version information about the Kubernetes cluster API server that your current `kubeconfig` context points to.

    ```bash
    dotnet kubeops api-version
    ```

*   **`install` (alias `i`)**: Installs Custom Resource Definitions (CRDs) into the connected cluster. It scans the specified solution or project file (`-s <path>`) for types decorated with `[KubernetesEntity]`, generates the corresponding CRD manifests using the transpiler logic, and applies them to the cluster.

    ```bash
    # Install CRDs defined in MyOperator.csproj into the current cluster
    dotnet kubeops install -s ./MyOperator.csproj
    ```

*   **`uninstall` (alias `u`)**: Uninstalls CRDs from the connected cluster. Like `install`, it scans the specified project/solution (`-s <path>`) to identify the CRDs managed by KubeOps and deletes them from the cluster.

    ```bash
    # Uninstall CRDs defined in MyOperator.csproj
    dotnet kubeops uninstall -s ./MyOperator.csproj
    ```

### Code & Manifest Generation
*   **`generate` (aliases `gen`, `g`)**: Parent command for various generation tasks.
    *   **`generate crd`**: Generates CRD YAML manifests for entities found in the specified solution/project (`-s <path>`) and outputs them to a specified directory (`-o <path>`). This does *not* apply them to the cluster; it only creates the files.

        ```bash
        # Generate CRD files for MyOperator.csproj into the './deploy' directory
        dotnet kubeops generate crd -s ./MyOperator.csproj -o ./deploy
        ```

    *   **`generate operator` (alias `op`)**: Generates a set of Kubernetes deployment manifests (e.g., Deployment, ServiceAccount, RBAC Roles/Bindings) necessary to deploy your operator. It inspects your operator project (`-s <path>`) and outputs the YAML files to a specified directory (`-o <path>`). This often includes manifests for any webhooks defined.

        ```bash
        # Generate deployment manifests for MyOperator.csproj into './deploy'
        dotnet kubeops generate operator -s ./MyOperator.csproj -o ./deploy
        ```

For more detailed usage examples and explanations, please refer to the main [KubeOps CLI Documentation](../../docs/cli.md).

### Example

When running `dotnet kubeops api-version`, your output may look like this:

```bash
> dotnet kubeops api-version
   Kubernetes API Version
   ┌─────────────┬─────────────┐
   │ Git-Version │ v1.29.1     │
   │ Major       │ 1           │
   │ Minor       │ 29          │
   │ Platform    │ linux/amd64 │
   └─────────────┴─────────────┘
