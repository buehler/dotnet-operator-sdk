# Resource Controller

When reconciling an entity of a `CRD`, one needs a controller to do so.
The controller abstracts the general complexity of watching the
resources on kubernetes and queueing of the events.

When you want to create a controller for your (or any) entity,
read the following instructions.

## Controller instance

After you created a custom entity (like described in [Entities](./entities.md))
or you want to reconcile a given entity (from the `k8s.Models` namespace,
e.g. `V1ConfigMap`) you need to create a controller class
as you would do for a MVC or API controller in asp.net.

Make sure you have the correct baseclass (`ResourceControllerBase<TEntity>`)
inherited.

```csharp
[EntityRbac(typeof(MyCustomEntity), Verbs = RbacVerb.All)]
public class FooCtrl: ResourceControllerBase<MyCustomEntity>
{
    protected override async Task<TimeSpan?> Created(MyCustomEntity resource){}
    // overwrite other methods here.
    // Possible overwrites:
    // "Created" (i.e. when the operator sees the entity for the first time),
    // "Updated" (i.e. when the operator knows the entity and it was updated),
    // "NotModified" (i.e. when nothing changed but a timed requeue happend),
    // "StatusModified" (i.e. when only the status was updated),
    // "Deleted" (i.e. when the entity was deleted and all finalizers are done)
}
```

## RBAC

The entity rbac attribute does provide the information needed about
your needed roles / rules.

Please configure all entities you want to manage with your
operator with such an entity rbac attribute. This generates
the rbac roles / role bindings for your operator and therefore
for the service account associated with the operator.

### EntityRbac

The first possibility to configure rbac is with the <xref:KubeOps.Operator.Rbac.EntityRbacAttribute>
attribute.

The attribute takes a list of types (your entities) and a <xref:KubeOps.Operator.Rbac.RbacVerb>.
The verbs define the needed permissions and roles for the given entity(ies).

You can configure multiple types and even well known entities from kubernetes:

```csharp
[EntityRbac(typeof(MyCustomEntity), Verbs = RbacVerb.All)]
[EntityRbac(typeof(V1Secret), typeof(V1ConfigMap), Verbs = RbacVerb.Get | RbacVerb.List)]
[EntityRbac(typeof(V1Deployment), Verbs = RbacVerb.Create | RbacVerb.Update | RbacVerb.Delete)]
```

### GenericRbac

The second possibility is to use the <xref:KubeOps.Operator.Rbac.GenericRbacAttribute>
which takes a list of api groups, resources, versions and a selection of
<xref:KubeOps.Operator.Rbac.RbacVerb>s to configure the rbac rule:

```csharp
[GenericRbac(Groups = new {"apps"}, Resources = new {"deployments"}, Verbs = RbacVerb.All)]
```

## Requeue

The controller's methods have a return value of `TimeSpan?`. This means
you can return a time span to automatically requeue the event for the
given entity. If requeued and nothing changed, it will most likely fire
a `NotModified` event.

This can be useful if you want to periodically check for a database
connection for example and update the status of a given entity.

If you return `null` in an event function, the event is not requeued.
If you return a timespan, then the event is requeued after this delay.

```csharp
/* snip... */
protected override async Task<TimeSpan?> Created(MyCustomEntity resource){
    // do something useful.
    return TimeSpan.FromSeconds(15); // This will retrigger an event in 15 secs.
}

protected override async Task<TimeSpan?> Updated(MyCustomEntity resource){
    // do something useful.
    return null; // This will not retrigger an event.
}
/* snip... */
```

## Error requeue

If the function throws an error, the event is requeued with an exponential backoff.

```csharp
/* snip... */
protected override async Task<TimeSpan?> Created(MyCustomEntity resource){
    // do something useful.
    throw new Exception("¯\\_(ツ)_/¯");
}
/* snip... */
```

The backoff function is defined as follows:

```csharp
private const double MaxRetrySeconds = 64;
private TimeSpan ExponentialBackoff(int retryCount) => TimeSpan
            .FromSeconds(Math.Min(Math.Pow(2, retryCount), MaxRetrySeconds))
            .Add(TimeSpan.FromMilliseconds(_rnd.Next(0, 1000)));
```

Which means with each retry, it calculates the new backoff time
to a max of 64. To each of those times a random number of milliseconds
is added to add a certain fuzzying.
