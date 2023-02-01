# KubeOps

![Code Security Testing](https://github.com/buehler/dotnet-operator-sdk/workflows/Code%20Security%20Testing/badge.svg)
![.NET Release](https://github.com/buehler/dotnet-operator-sdk/workflows/.NET%20Release/badge.svg)
![.NET Testing](https://github.com/buehler/dotnet-operator-sdk/workflows/.NET%20Testing/badge.svg)
[![Nuget](https://img.shields.io/nuget/v/KubeOps)](https://www.nuget.org/packages/KubeOps/)
[![Nuget](https://img.shields.io/nuget/vpre/KubeOps?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps/absoluteLatest)

This is the repository of "KubeOps" - The dotnet Kubernetes Operator SDK.

The documentation moved to respective `README` files in the packages.
Please consult them via nuget explorer, on the GitHub repository, or on the links
below.

## Packages

- [KubeOps](./kubeops.html) - The core package of the SDK.
- [KubeOps.KubernetesClient](./kubeops-kubernetesclient.html) - An improved Kubernetes client to interact with Kubernetes APIs.
- [KubeOps.Templates](./kubeops-templates.html) - `dotnet new` templates for creating operators.
- [KubeOps.Testing](./kubeops-testing.html) - Extensions that support integration testing.

## Contribution

If you want to contribute, feel free to open a pull request or write issues :-)
Read more about contribution (especially for setting up your local environment)
in the [CONTRIBUTING file](./contribution.html).

Short wrapup:
- Check out the code
- Develop on KubeOps
- Use some Kubernetes to run the test operator against
- Create tests
- Build the whole solution (lint warnings will result in an error)
- Open PR

## Motivation

The motivation was to learn more about the quirks of kubernetes itself and
provide an alternative to kubebuilder and operator sdk which are both
written in GoLang.
