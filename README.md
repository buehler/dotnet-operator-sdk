# KubeOps

![Code Security Testing](https://github.com/buehler/dotnet-operator-sdk/workflows/Code%20Security%20Testing/badge.svg)
![.NET Release](https://github.com/buehler/dotnet-operator-sdk/workflows/.NET%20Release/badge.svg)
![.NET Testing](https://github.com/buehler/dotnet-operator-sdk/workflows/.NET%20Testing/badge.svg)
[![Nuget](https://img.shields.io/nuget/v/KubeOps)](https://www.nuget.org/packages/KubeOps/)
[![Nuget](https://img.shields.io/nuget/vpre/KubeOps?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps/absoluteLatest)

This is the repository of "KubeOps" - The dotnet Kubernetes Operator SDK.

The documentation is provided in the code itself (description of the methods and classes)
and each package contains a README.md with further information/documentation.

## Packages

- [KubeOps](./src/KubeOps/README.md) - The core package of the SDK.
- [KubeOps.Testing](./src/KubeOps.Testing/README.md) - Extensions that support integration testing.
- [KubeOps.Templates](./src/KubeOps.Templates/README.md) - `dotnet new` templates for creating operators.
- [KubeOps.KubernetesClient](./src/KubeOps.KubernetesClient/README.md) - An improved Kubernetes client to interact with Kubernetes APIs.

## Contribution

If you want to contribute, feel free to open a pull request or write issues :-)

## Motivation

The motivation was to learn more about the quirks of kubernetes itself and
provide an alternative to kubebuilder and operator sdk which are both
written in GoLang.
