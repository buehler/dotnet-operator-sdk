# Events / Event Series

Kubernetes knows "Events" which can be sort of attached to a resource
(i.e. a Kubernetes object).

To create and use events, inject the @"KubeOps.Operator.Events.IEventManager"
into your controller. It is registered as a transient resource in the DI
container.

## IEventManager

### Publish events

The event manager allows you to either publish an event that you created
by yourself, or helps you publish events with predefined data.

If you want to use the helper:
```c#
// fetch from DI, or inject into your controller.
IEventManager manager = services.GetRequiredService<IEventManager>;

// Publish the event.
// This creates an event and publishes it.
// If the event was previously published, it is fetched
// and the "count" number is increased. This essentially
// creates an event-series.
await manager.Publish(resource, "reason", "my fancy message");
```

If you want full control over the event:
```c#
// fetch from DI, or inject into your controller.
IEventManager manager = services.GetRequiredService<IEventManager>;

var @event = new Corev1Event
    {
        // ... fill out all fields.
    }

// Publish the event.
// This essentially calls IKubernetesClient.Save.
await manager.Publish(@event);
```

### Use publisher delegates

If you don't want to call the
@"KubeOps.Operator.Events.IEventManager.Publish(k8s.IKubernetesObject{k8s.Models.V1ObjectMeta},System.String,System.String,KubeOps.Operator.Events.EventType)"
all the time with the same arguments, you can create delegates.

There exist two different delegates:
- @"KubeOps.Operator.Events.IEventManager.StaticPublisher": Predefined event
  on a predefined resource.
- @"KubeOps.Operator.Events.IEventManager.Publisher": Predefined event
  on a variable resource.

Both are created with their specific overload:
- @"KubeOps.Operator.Events.IEventManager.CreatePublisher(k8s.IKubernetesObject{k8s.Models.V1ObjectMeta},System.String,System.String,KubeOps.Operator.Events.EventType)"
- @"KubeOps.Operator.Events.IEventManager.CreatePublisher(System.String,System.String,KubeOps.Operator.Events.EventType)"

To use the static publisher:
```c#
var publisher = manager.CreatePublisher(resource, "reason", "message");
await publisher();

// and later on:
await publisher(); // again without specifying reason / message and so on.
```

To use the dynamic publisher:
```c#
var publisher = manager.CreatePublisher("reason", "message");
await publisher(resource);

// and later on:
await publisher(resource); // again without specifying reason / message and so on.
```

The dynamic publisher can be used to predefine the event for your resources.

As an example in a controller:
```c#
public class TestController : ResourceControllerBase<V1TestEntity>
{
    private readonly IEventManager.Publisher _publisher;

    public TestController(IEventManager eventManager, IResourceServices<V1TestEntity> services)
        : base(services)
    {
        _publisher = eventManager.CreatePublisher("reason", "my fancy message");
    }

    protected override async Task<TimeSpan?> Created(V1TestEntity resource)
    {
        // Here, the event is published with predefined strings
        // but for a "variable" resource.
        await _publisher(resource);
        return await base.Created(resource);
    }
}
```
