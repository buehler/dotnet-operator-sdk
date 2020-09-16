TODO

### Write Finalizers

A finalizer can be as simple as:

```csharp
public class FooFinalizer : ResourceFinalizerBase<Foo>
{
    public override async Task Finalize(Foo resource)
    {
        // do something with the resource.
    }
}
```

And can be added to a resource with:

```csharp
// in a resource controller
await AttachFinalizer<TestEntityFinalizer>(resource);
```

After the finalizer ran successfully on a resource, it is unregistered
on the resource.
