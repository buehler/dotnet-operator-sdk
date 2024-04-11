# KubeOps Operator

The `KubeOps.Operator` package provides a framework
for building Kubernetes operators in .NET.
It is built on top of the Kubernetes client libraries for .NET
and provides a set of abstractions and utilities for implementing
operators that manage custom resources in a Kubernetes cluster.

## Getting Started

To get started with the SDK, you can install it from NuGet:

```bash
dotnet add package KubeOps.Operator
```

Once you have installed the package, you can create entities,
controllers, finalizers, and more to implement your operator.

All resources must be added to the operator builder
in order to be recognized by the SDK and to be used as
operator resources. The [KubeOps.Generator](../KubeOps.Generator/README.md)
helps with convenience methods to register everything
at once.

You'll need to use the Generic Host to run your operator.
However, for a plain operator without webhooks, no ASP.net
is required (in contrast to v7).

```csharp
using KubeOps.Operator;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Trace);

builder.Services
    .AddKubernetesOperator()
    .RegisterComponents();

using var host = builder.Build();
await host.RunAsync();
```

### Registering Resources

When using the [KubeOps.Generator](../KubeOps.Generator/README.md),
you can use the `RegisterResources` function:

```csharp
builder.Services
    .AddKubernetesOperator()
    .RegisterComponents();
```

Otherwise, you can register resources manually:

```csharp
builder.Services
    .AddKubernetesOperator()
    .AddController<TestController, V1TestEntity>()
    .AddFinalizer<FirstFinalizer, V1TestEntity>("first")
    .AddFinalizer<SecondFinalizer, V1TestEntity>("second")
```

### Entity

To create an entity, you need to implement the
`IKubernetesObject<V1ObjectMeta>` interface. There are convenience
classes available to help with initialization, status and spec
properties.

```csharp
[KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
public class V1TestEntity :
    CustomKubernetesEntity<V1TestEntity.EntitySpec, V1TestEntity.EntityStatus>
{
    public override string ToString()
    => $"Test Entity ({Metadata.Name}): {Spec.Username} ({Spec.Email})";

    public class EntitySpec
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }

    public class EntityStatus
    {
        public string Status { get; set; } = string.Empty;
    }
}
```

### Controller

A controller is the element that reconciles a specific entity.
You can reconcile your own custom entities or all other entities
as long as they are registered within the SDK. For a guide
on how to reconcile external entities, refer to the
[documentation](https://buehler.github.io/dotnet-operator-sdk/).

A simple controller could look like this:

```csharp
[EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
public class V1TestEntityController : IEntityController<V1TestEntity>
{
    private readonly IKubernetesClient _client;
    private readonly EntityFinalizerAttacher<FinalizerOne, V1TestEntity> _finalizer1;

    public V1TestEntityController(
        IKubernetesClient client,
        EntityFinalizerAttacher<FinalizerOne, V1TestEntity> finalizer1)
    {
        _client = client;
        _finalizer1 = finalizer1;
    }

    public async Task ReconcileAsync(V1TestEntity entity)
    {
        _logger.LogInformation("Reconciling entity {Entity}.", entity);

        entity = await _finalizer1(entity);

        entity.Status.Status = "Reconciling";
        entity = await _client.UpdateStatus(entity);
        entity.Status.Status = "Reconciled";
        await _client.UpdateStatus(entity);
    }
}
```

This controller attaches a specific finalizer to the entity,
updates its status and then saves the entity.

> [!CAUTION]
> It is important to always use the returned values
> of an entity when using modifying actions of the
> Kubernetes client. Otherwise, you will receive
> "HTTP CONFLICT" errors because of the resource version
> field in the entity.

> [!NOTE]
> Do not update the entity itself in the reconcile loop.
> It is considered bad practice to update entities
> while reconciling them. However, the status may be updated.
> To update entities before they are reconciled
> (e.g. to ban certain values or change values),
> use webhooks instead.

### Finalizer

A [finalizer](https://kubernetes.io/docs/concepts/overview/working-with-objects/finalizers/)
is an element for asynchronous cleanup in Kubernetes.

It is attached with an `EntityFinalizerAttacher` and is called
when the entity is marked as deleted.

```csharp
public class FinalizerOne : IEntityFinalizer<V1TestEntity>
{
    public Task FinalizeAsync(V1TestEntity entity)
    {
        return Task.CompletedTask;
    }
}
```

> [!NOTE]
> The controller (if you overwrote the `DeletedAsync` method)
> will receive the notification as soon as all finalizers
> are removed.

## Documentation

For more information, please visit the
[documentation](https://buehler.github.io/dotnet-operator-sdk/).
