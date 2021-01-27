# KubeOps - The Kubernetes Operator SDK

![Code Security Testing](https://github.com/buehler/dotnet-operator-sdk/workflows/Code%20Security%20Testing/badge.svg)
![.NET Release](https://github.com/buehler/dotnet-operator-sdk/workflows/.NET%20Release/badge.svg)
![.NET Testing](https://github.com/buehler/dotnet-operator-sdk/workflows/.NET%20Testing/badge.svg)
[![Nuget](https://img.shields.io/nuget/v/KubeOps)](https://www.nuget.org/packages/KubeOps/)
[![Nuget](https://img.shields.io/nuget/vpre/KubeOps?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps/absoluteLatest)

Welcome to the documentation of `KubeOps`.

This package (sadly "DotnetOperatorSdk" is already taken on nuget, so its "KubeOps")
is a kubernetes operator sdk written in dotnet. It is heavily inspired by
["kubebuilder"](https://github.com/kubernetes-sigs/kubebuilder)
that provides the same and more functions for kubernetes operators in GoLang.

The goal was to learn about resource watching in .net and provide a neat way of writing a
custom operator yourself.

The motivation was to learn more about the quirks of kubernetes itself and
provide an alternative to kubebuilder and operator sdk which are both
written in GoLang.

Please head over to the [documentation](./docs/getting_started.md) to see how to get started
and what features are hiding in KubeOps.
