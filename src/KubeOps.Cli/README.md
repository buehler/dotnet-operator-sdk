# KubeOps CLI

The KubeOps CLI (`dotnet kubeops`) is a [.NET Tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) designed to streamline common development tasks when building Kubernetes operators with KubeOps. It helps with generating Custom Resource Definition (CRD) manifests, installing/uninstalling those CRDs in a cluster, generating deployment manifests, and inspecting your cluster.

## Installation

The KubeOps CLI can be installed either locally within a specific repository or globally on your machine.

**Local Installation (Recommended for Projects):**

First, ensure you have a tool manifest file (usually `.config/dotnet-tools.json`). If not, create one:

```bash
dotnet new tool-manifest
```

Then install the CLI:

```bash
dotnet tool install KubeOps.Cli
```

This installs and pins the tool version for the current repository. Invoke it using `dotnet kubeops <command>`.

**Global Installation (Convenient for General Use):**

```bash
dotnet tool install --global KubeOps.Cli
```

This makes the `dotnet kubeops` command available anywhere on your system. Use `dotnet tool update --global KubeOps.Cli` to get the latest version.

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
*   **`new`**: Scaffolds new KubeOps projects.
    *   **`new operator`**: Creates a new operator project from a template, including a basic custom resource, controller, and finalizer.

        ```bash
        # Create a new operator project in a directory named 'MyNewOperator'
        dotnet kubeops new operator MyNewOperator
        ```

*   **`generate` (aliases `gen`, `g`)**: Parent command for various generation tasks.
    *   **`generate crd`**: Generates CRD YAML manifests for entities found in the specified solution/project (`-s <path>`) and outputs them to a specified directory (`-o <path>`). This does *not* apply them to the cluster; it only creates the files.
        *   If `-o` is omitted, output defaults to the current directory.
        *   This command uses the [KubeOps.Transpiler](../KubeOps.Transpiler/README.md) package to convert C# entity definitions to CRDs.

        ```bash
        # Generate CRD files for MyOperator.csproj into the './deploy' directory
        dotnet kubeops generate crd -s ./MyOperator.csproj -o ./deploy
        ```

    *   **`generate operator` (alias `op`)**: Generates a set of Kubernetes deployment manifests (e.g., Deployment, ServiceAccount, RBAC Roles/Bindings) necessary to deploy your operator. It inspects your operator project (`-s <path>`) and outputs the YAML files to a specified directory (`-o <path>`). This often includes manifests for any webhooks defined.
        *   If `-o` is omitted, output defaults to the current directory.
        *   This command inspects `[EntityRbac]` attributes on controllers/webhooks to generate `Role` and `ClusterRole` resources.
        *   It also finds webhook implementations (`[ValidationWebhook]`, `[MutationWebhook]`, `[ConversionWebhook]`) to generate corresponding `ValidatingWebhookConfiguration`, `MutatingWebhookConfiguration`, and CRD `conversion` settings.
        *   It utilizes the [KubeOps.Transpiler](../KubeOps.Transpiler/README.md) package for analyzing attributes.

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
