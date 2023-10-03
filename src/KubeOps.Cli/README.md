# KubeOps CLI

.NET tool to help managing your KubeOps project.
It allows you to generate the needed files for your operator
as well as managing resources in your Kubernetes cluster.

## Installation

To install the CLI into your solution / project, first create a new tool manifest:
```bash
dotnet new tool-manifest
```

Then install the CLI:
```bash
dotnet tool install KubeOps.Cli
```

This allows you to use the CLI with `dotnet kubeops`.

## Available Commands

Here is a brief overview over the available commands
(for all options and descriptions, use `-h` or `--help`):

- `version`: prints the version information for the actual connected Kubernetes cluster
- `install`: install the CRDs for the given solution into the cluster
- `uninstall`: uninstall the CRDs for the given solution from the cluster
- `generator`: entry command for generator commands (i.e. has subcommands), all commands
  output their result to the stdout or the given output path
    - `cert`: generate a CA & server certificate for usage with webhooks
    - `crd`: generate the CRDs
    - `docker`: generate the dockerfile
    - `installer`: generate the installer files (i.e. kustomization yaml) for the operator
    - `operator`: generate the deployment for the operator
    - `rbac`: generate the needed rbac roles / role bindings for the operator
- `webhook`: entry command for webhook related operations
    - `install`: generate the server certificate and install the service / webhook registration
    - `register`: register the currently implemented webhooks to the currently selected cluster

### Example

When running `dotnet kubeops api-version`, your output may look like this:

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
