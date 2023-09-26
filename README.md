# KubeOps

![Code Security Testing](https://github.com/buehler/dotnet-operator-sdk/workflows/Code%20Security%20Testing/badge.svg)
![.NET Release](https://github.com/buehler/dotnet-operator-sdk/workflows/.NET%20Release/badge.svg)
![.NET Testing](https://github.com/buehler/dotnet-operator-sdk/workflows/.NET%20Testing/badge.svg)

This is the repository of "KubeOps" - The dotnet Kubernetes Operator SDK.

The documentation is provided in the code itself (description of the methods and classes)
and each package contains a README with further information/documentation.

Also, there is a `docfx` site that provides further documentation and examples.
You can find it [here](https://buehler.github.io/dotnet-operator-sdk/).

## Packages

The following packages exist:

| Package                                                      | Description                                         | Version                                                                                                               | Pre Version                                                                                                                                                     |
|--------------------------------------------------------------|-----------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [KubeOps.Abstractions](./src/KubeOps.Abstractions/README.md) | Contains abstractions, attributes, etc. for the SDK | [![Nuget](https://img.shields.io/nuget/v/KubeOps.Abstractions)](https://www.nuget.org/packages/KubeOps.Abstractions/) | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Abstractions?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Abstractions/absoluteLatest) |
| [KubeOps.Cli](./src/KubeOps.Cli/README.md)                   | CLI Dotnet Tool to generate stuff                   | [![Nuget](https://img.shields.io/nuget/v/KubeOps.Cli)](https://www.nuget.org/packages/KubeOps.Cli/)                   | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Cli?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Cli/absoluteLatest)                   |
| [KubeOps.Generator](./src/KubeOps.Generator/README.md)       | Source Generator for the SDK                        | [![Nuget](https://img.shields.io/nuget/v/KubeOps.Generator)](https://www.nuget.org/packages/KubeOps.Generator/)       | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Generator?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Generator/absoluteLatest)       |
| [KubeOps.Operator](./src/KubeOps.Operator/README.md)         | Main SDK entrypoint to create an operator           | [![Nuget](https://img.shields.io/nuget/v/KubeOps.Operator)](https://www.nuget.org/packages/KubeOps.Operator/)         | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Operator?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Operator/absoluteLatest)         |
| [KubeOps.Operator.Web](./src/KubeOps.Operator.Web/README.md) | Web part of the operator (for webhooks)             | [![Nuget](https://img.shields.io/nuget/v/KubeOps.Operator.Web)](https://www.nuget.org/packages/KubeOps.Operator.Web/) | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Operator.Web?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Operator.Web/absoluteLatest) |
| [KubeOps.Transpiler](./src/KubeOps.Transpiler/README.md)     | Transpilation helpers for CRDs and RBAC elements    | [![Nuget](https://img.shields.io/nuget/v/KubeOps.Transpiler)](https://www.nuget.org/packages/KubeOps.Transpiler/)     | [![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Transpiler?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Transpiler/absoluteLatest)     |

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
