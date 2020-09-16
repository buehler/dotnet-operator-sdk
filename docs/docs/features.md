# Features

This is a general list of features. For specific documentation, have a look
at the sections.

As of now, the operator sdk supports - roughly - the following features:

- Controller with all operations of an entity
  - Created
  - Updated
  - NotModified
  - StatusModified
  - Deleted
  - AddFinalizer
- Finalizers for entities
- Prometheus metrics for queues / caches / watchers
- Healthchecks, split up to "readiness" and "liveness" (or both)
- Commands for the operator (for exact documentation run: `dotnet run -- --help`)
  - `Run`: Start the operator and run the asp.net application
  - `Install`: Install the found CRD's into the actual configured
    cluster in your kubeconfig
  - `Uninstall`: Remove the CRDs from your cluster
  - `Generate CRD`: Generate the yaml for your CRDs
  - `Generate Docker`: Generate a dockerfile for your operator
  - `Generate Installer`: Generate a kustomization yaml for your operator
  - `Generate Operator`: Generate the yaml for your operator (rbac / role / etc)
  - `Generate RBAC`: Generate rbac roles for your CRDs

Other features and ideas are listed in the repositories
["issues"](https://github.com/buehler/dotnet-operator-sdk/issues).
