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
@"KubeOps.Testing.KubernetesOperatorFactory`1". It is ment to be
injected into your test.

> [!NOTE]
> The following documentation assumes you are using xUnit as a
> testing framework. The techniques used should be present
> with other testing frameworks as well.

The following steps are needed for integration testing the controller:

- Create a `TestStartup.cs` file (or any other name you want)
- Inject the @"KubeOps.Testing.KubernetesOperatorFactory`1"
- Use the mocked client / queues to test your operator

## Test Startup

This file is very similar to a "normal" `Startup.cs` file of an
asp.net application. Either you subclass it and replace your test mocked
services, or you create a new one.

[!code-csharp[TestStartup.cs](../../tests/KubeOps.TestOperator.Test/TestStartup.cs?highlight=20-21)]

## Mocked elements

The main part of this operator factory does mock the used kubernetes client
and the resource event queues.

Both mocked elements can be retrieved via the operator factory with:

- @"KubeOps.Testing.KubernetesOperatorFactory`1.GetMockedEventQueue``1"
- @"KubeOps.Testing.KubernetesOperatorFactory`1.MockedKubernetesClient"

### Mocked event queue

The event queue does not use a channel to read and write event but directly fire
the given events.

Also, the @"KubeOps.Operator.Queue.IResourceEventQueue`1.Enqueue(`0,System.Nullable{System.TimeSpan})"
method does not actually enqueue anything but just adds the resource to a list
of resources.

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

[!code-csharp[TestStartup.cs](../../tests/KubeOps.TestOperator.Test/TestController.Test.cs?range=10-31,84&highlight=7,13)]
