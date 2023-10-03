﻿# KubeOps

[![.NET Pre-Release](https://github.com/buehler/dotnet-operator-sdk/actions/workflows/dotnet-release.yml/badge.svg?branch=main)](https://github.com/buehler/dotnet-operator-sdk/actions/workflows/dotnet-release.yml)
[![.NET Release](https://github.com/buehler/dotnet-operator-sdk/actions/workflows/dotnet-release.yml/badge.svg?branch=release)](https://github.com/buehler/dotnet-operator-sdk/actions/workflows/dotnet-release.yml)
[![Scheduled Code Security Testing](https://github.com/buehler/dotnet-operator-sdk/actions/workflows/security-analysis.yml/badge.svg?event=schedule)](https://github.com/buehler/dotnet-operator-sdk/actions/workflows/security-analysis.yml)

This is the repository of "KubeOps" - The dotnet Kubernetes Operator SDK.

The documentation is provided in the code itself (description of the methods and classes)
and each package contains a README with further information/documentation. For a more
detailed documentation, head to the [GitHub Pages](https://buehler.github.io/dotnet-operator-sdk/).

## Packages

The following packages exist:

| Package                                                              | Description                                            | Framework Support                           | Latest Version                                                                                                                                                          |
|----------------------------------------------------------------------|--------------------------------------------------------|---------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [KubeOps.Abstractions](./src/KubeOps.Abstractions/README.md)         | Contains abstractions, attributes, etc. for the SDK    | netstandard2.0 netstandard2.1 net6.0 net7.0 | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Abstractions?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Abstractions/absoluteLatest)         |
| [KubeOps.Cli](./src/KubeOps.Cli/README.md)                           | CLI Dotnet Tool to generate stuff                      | net7.0                                      | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Cli?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Cli/absoluteLatest)                           |
| [KubeOps.Generator](./src/KubeOps.Generator/README.md)               | Source Generator for the SDK                           | netstandard2.0                              | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Generator?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Generator/absoluteLatest)               |
| [KubeOps.KubernetesClient](./src/KubeOps.KubernetesClient/README.md) | Extended client to communicate with the Kubernetes API | net6.0 net7.0                               | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.KubernetesClient?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.KubernetesClient/absoluteLatest) |
| [KubeOps.Operator](./src/KubeOps.Operator/README.md)                 | Main SDK entrypoint to create an operator              | net6.0 net7.0                               | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Operator?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Operator/absoluteLatest)                 |
| [KubeOps.Operator.Web](./src/KubeOps.Operator.Web/README.md)         | Web part of the operator (for webhooks)                | net6.0 net7.0                               | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Operator.Web?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Operator.Web/absoluteLatest)         |
| [KubeOps.Transpiler](./src/KubeOps.Transpiler/README.md)             | Transpilation helpers for CRDs and RBAC elements       | netstandard2.0 netstandard2.1 net6.0 net7.0 | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Transpiler?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Transpiler/absoluteLatest)             |

## Contribution

If you want to contribute, feel free to open a pull request or write issues :-)
Read more about contribution (especially for setting up your local environment)
in the [CONTRIBUTING file](./CONTRIBUTING.md).

In short:

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
