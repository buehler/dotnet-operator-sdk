# Contributing to KubeOps

First of all, thank you for considering contributing to KubeOps.
This is an open souce project and shall be driven by the community.

This document describes how contributions may be done and what is required
to develop on KubeOps.

## Creating/Reporting Issues

Feel free to open an issue in the [issues section](https://github.com/buehler/dotnet-operator-sdk/issues).
There are three issue templates:
- Bug: to report an issue/bug that prevents usage or is an inconvenience of KubeOps
- Feature request: to report a new feature that would enhance KubeOps
- Documentation: to report missing / wrong documentation

Please search through the already created issues to find similarities.

## Creating Pull Requests

To directly contribute to the solution, create a fork of the repository
and implement your addition. Please keep in mind that reviewing takes some
time and is not done instantly.

Please adhere to the linting rules and the general code style in the repository.
Also, add tests for your changes to ensure that the system works well
when other changes happen.

The PR can have any name, but it would be nice if you adhere to
the repositories standard naming. Please name your PR
with [Convential Commits](https://www.conventionalcommits.org/en/v1.0.0/#summary).

**NOTE for breaking changes**: please state breaking changes
in the PR description. The review process will be faster when
breaking changes are well documented.

A few examples:
- "fix: Null exception during watcher process"
- "feat(core): Add new functionality"
- "feat(testing): expose kubernetes client for testing"
- "refactor: changed this and that"
- "docs: Add docs about KubeOps core"

The PR will be squashed and merged into the default branch.

## Local Development

To setup a local development environment, you'll need to perform the follwing steps:

- Check out the repository (or your fork)
- If you want to run the Operator locally, you'll need some Kubernetes instance.
  This can be any Kubernetes instance you'd like:
  - Local Kubernetes in Docker for Mac/Windows
  - minikube / any other local Kubernetes
  - Deployed Kubernetes (e.g. GCP Kubernetes instance)
- You can now code your stuff.
- `tests/KubeOps.TestOperator` is a developed small operator that can be run
  locally to test your implementations.
- Write tests for your changes
- Build the whole solution and check for linting errors / warnings.
  **NOTE** that any warning will result in an error when building
  with `Release` configuration.
- Do not change the linting rules without creating a discussion/issue first.
