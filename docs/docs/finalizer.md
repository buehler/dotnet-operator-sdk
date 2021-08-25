# Finalizers

A finalizer is a special type of software that can asynchronously
cleanup stuff for an entity that is being deleted.

A finalizer is registered as an identifier in a kubernetes
object (i.e. in the yaml / json structure) and the object
wont be removed from the api until all finalizers are removed.

If you write finalizer, they will be automatically added to the
DI system via their type <xref:KubeOps.Operator.Finalizer.IResourceFinalizer`1>

## Write a finalizer

Use the correct interface (<xref:KubeOps.Operator.Finalizer.IResourceFinalizer`1>).

A finalizer can be as simple as:

```csharp
public class TestEntityFinalizer : IResourceFinalizer<V1TestEntity>
{
    private readonly IManager _manager;

    public TestEntityFinalizer(IManager manager)
    {
        _manager = manager;
    }

    public Task FinalizeAsync(V1TestEntity resource)
    {
        _manager.Finalized(resource);
        return Task.CompletedTask;
    }
}
```

The interface also provides a way of overwriting the
<xref:KubeOps.Operator.Finalizer.IResourceFinalizer`1.Identifier> of the finalizer if you feed like it.

When the finalizer successfully completed his job, it is automatically removed
from the finalizers list of the entity. The finalizers are registered
as scoped resources in DI.

## Register a finalizer

To attach a finalizer for a resource, call the
<xref:KubeOps.Operator.Finalizer.IFinalizerManager` 1.RegisterFinalizerAsync``1( `0)>
method in the controller during reconciliation.

```csharp
public class TestController : IResourceController<V1TestEntity>
{
    private readonly IFinalizerManager<V1TestEntity> _manager;

    public TestController(IFinalizerManager<V1TestEntity> manager)
    {
        _manager = manager;
    }

    public async Task<ResourceControllerResult> CreatedAsync(V1TestEntity resource)
    {
        // The type MyFinalizer must be an IResourceFinalizer<V1TestEntity>
        await _manager.RegisterFinalizerAsync<MyFinalizer>(resource);
        return null;
    }
}
```

Alternatively, the <xref:KubeOps.Operator.Finalizer.IFinalizerManager`1.RegisterAllFinalizersAsync(`0)>
method can be used to attach all finalizers known to the operator for that entity type.

```csharp
public class TestController : IResourceController<V1TestEntity>
{
    private readonly IFinalizerManager<V1TestEntity> _manager;

    public TestController(IFinalizerManager<V1TestEntity> manager)
    {
        _manager = manager;
    }

    public async Task<ResourceControllerResult> CreatedAsync(V1TestEntity resource)
    {
        await _manager.RegisterAllFinalizersAsync(resource);
        return null;
    }
}
```

## Unregistering a finalizer

When a resource is finalized, the finalizer is removed automatically.
However, if you want to remove a finalizer before a resource is deleted/finalized,
you can use <xref:KubeOps.Operator.Finalizer.IFinalizerManager` 1.RemoveFinalizerAsync``1( `0)>.

```csharp
public class TestController : IResourceController<V1TestEntity>
{
    private readonly IFinalizerManager<V1TestEntity> _manager;

    public TestController(IFinalizerManager<V1TestEntity> manager)
    {
        _manager = manager;
    }

    public async Task<ResourceControllerResult> CreatedAsync(V1TestEntity resource)
    {
        await _manager.RemoveFinalizerAsync<MyFinalizer>(resource);
        return null;
    }
}
```
