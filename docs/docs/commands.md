TODO

## Commands

There are default command line commands which you can see when using
`dotnet run -- --help` in your project. As you can see, you can run
multiple commands. Some of them do install / uninstall your crds in
your currently selected kubernetes cluster or can generate code.

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
