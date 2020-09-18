# Finalizers

A finalizer is a special type of software that can asynchronously
cleanup stuff for an entity that is beeing deleted.

A finalizer is registered as an identifier in the kubernetes
resource (i.e. in the yaml / json structure) and an object
wont be removed from the api until all finalizers are removed.

If you write finalizer, don't forget to register them with
<xref:KubeOps.Operator.Builder.IOperatorBuilder.AddFinalizer``1>
to the DI system.

## Write a finalizer

Use the correct base class (<xref:KubeOps.Operator.Finalizer.ResourceFinalizerBase`1>).

A finalizer can be as simple as:

```csharp
public class FooFinalizer : ResourceFinalizerBase<Foo>
{
    public override async Task Finalize(Foo resource)
    {
        // do something with the resource.
        // like deleting a database.
    }
}
```

When the finalizer successfully completed his job, it is automatically removed
from the finalizers list of the resource. The finalizers are registered
as transient resources in DI.

## Register a finalizer

To attach a finalizer for a resource, call the
<xref:KubeOps.Operator.Controller.ResourceControllerBase`1.AttachFinalizer``1(`0)>
method in the controller during reconciliation.

```csharp
public class TestController : ResourceControllerBase<V1TestEntity>
{
    protected override async Task<TimeSpan?> Created(V1TestEntity resource)
    {
        await AttachFinalizer<TestEntityFinalizer>(resource);
        return await base.Created(resource);
    }
}
```
