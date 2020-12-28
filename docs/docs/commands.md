# Commands

For convenience, there are multiple commands added to the executable
of your operator (through the KubeOps package).

Those are implemented with the [CommandLineUtils by NateMcMaster](https://github.com/natemcmaster/CommandLineUtils).

you can see the help and overview when using
`dotnet run -- --help` in your project. As you can see, you can run
multiple commands. Some of them do install / uninstall your crds in
your currently selected kubernetes cluster or can generate code.

> [!NOTE]
> For the normal "dotnet run" command exists a `--namespaced`
> option that starts the operator in namespaced mode. This means
> that only the given namespace is watched for entities.

## Available Commands

Here is a brief overview over the available commands:

> [!NOTE]
> all commands assume either the compiled dll or you using
> `dotnet run -- ` as prepended command.

- `""` (empty): runs the operator (normal `dotnet run`)
- `version`: prints the version information for the actual connected kubernetes cluster
- `install`: install the CRDs for the solution into the cluster
- `uninstall`: uninstall the CRDs for the solution from the cluster
- `generator`: entry command for generator commands (i.e. has subcommands), all commands
  output their result to the stdout or the given output path
  - `crd`: Generate the CRDs
  - `docker`: Generate the dockerfile
  - `installer`: Generate the installer files (i.e. kustomization yaml) for the operator
  - `operator`: Generate the deployment for the operator
  - `rbac`: Generate the needed rbac roles / role bindings for the operator

## Code Generation

When installing this package, you also reference the default Targets and Props
that come with the build engine. While building the following elements are generated:

- Dockerfile (if not already present)
- CRDs for your custom entities
- RBAC roles and role bindings for your requested resources
- Deployment files for your operator
- Installation file for your operator (kustomize)

The dockerfile will not be overwritten in case you have custom elements in there.
The installation files won't be overwritten as well if you have custom elements in there.

To regenerate those two elements, just delete them and rebuild your code.

For the customization on those build targets, header over to the
[ms build extensions](./ms_build.md).
