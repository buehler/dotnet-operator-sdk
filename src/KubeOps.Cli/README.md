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

- `api-version`, `av`: Prints the version information for the actual connected Kubernetes cluster
- `install`, `i`: Install the CRDs for the given solution into the cluster
- `uninstall`, `u`: Uninstall the CRDs for the given solution from the cluster
- `generate`, `gen`, `g`: Generates elements related to the operator
  - `operator`, `op`: Generate the all files necessary for deployment for the operator, including any webhooks

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
