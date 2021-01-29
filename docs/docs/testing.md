# Test your operator

The `KubeOps.Testing` package provides you with some tools
to aid with the testing of the custom operator.

To have an example for integration testing a custom operator
have a look at the test code in the repository:
[Integration Tests](https://github.com/buehler/dotnet-operator-sdk/tree/master/tests/KubeOps.TestOperator)

> [!NOTE]
> The tools provided aid with Integration testing.
> For normal unit testing, you can just mock all the things.

The main entry point for testing your custom operator is the
@"KubeOps.Testing.KubernetesOperatorFactory`1". It is meant to be
injected into your test.

> [!NOTE]
> The following documentation assumes you are using xUnit as a
> testing framework. The techniques used should be present
> with other testing frameworks as well.

The following steps are needed for integration testing the controller:

- Create a `TestStartup.cs` file (or any other name you want)
- Inject the @"KubeOps.Testing.KubernetesOperatorFactory`1"
- Use the mocked client and helper functions to test your operator

## Test Startup

This file is very similar to a "normal" `Startup.cs` file of an
asp.net application. Either you subclass it and replace your test mocked
services, or you create a new one.

[!code-csharp[TestStartup.cs](../../tests/KubeOps.TestOperator.Test/TestStartup.cs)]

## Mocked elements

The main part of this operator factory does mock the used kubernetes client
and helper functions to enqueue events and finalizer events.

Both mocked elements can be retrieved via the operator factory with:

- @"KubeOps.Testing.KubernetesOperatorFactory`1.MockedKubernetesClient"
- @"KubeOps.Testing.KubernetesOperatorFactory`1.EnqueueEvent``1(KubeOps.Operator.Kubernetes.ResourceEventType,``0)"
- @"KubeOps.Testing.KubernetesOperatorFactory`1.EnqueueFinalization``1(``0)"

### Mocked events

With the mentioned factory functions (@"KubeOps.Testing.KubernetesOperatorFactory`1.EnqueueEvent``1(KubeOps.Operator.Kubernetes.ResourceEventType,``0)"
and @"KubeOps.Testing.KubernetesOperatorFactory`1.EnqueueFinalization``1(``0)")
one can fire events for entities. This can be used to test your controllers.

The controllers are normally instantiated via the scope of the DI system.

### Mocked IKubernetesClient

The client is essentially an empty implementation of the interface.
All methods do return `null` or the object that was passed
into the mocked class. There are five different object
references that can be set to return certain results upon calling the client.

> [!WARNING]
> The mocked client is injected as singleton, this means
> the used "result" references can vary. Be aware of that
> and don't run your tests in parallel if you rely
> on those results.

## Writting a test

Now with all parts in place, we can write a test.
You probably need to set the solution relative content root for
asp.net testing. But then you can run the factory
and create a test.

[!code-csharp[TestStartup.cs](../../tests/KubeOps.TestOperator.Test/TestController.Test.cs)]
