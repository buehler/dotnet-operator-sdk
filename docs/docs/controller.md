# Resource Controller

When reconciling an entity of a `CRD`, one needs a controller to do so.
The controller abstracts the general complexity of watching the
resources on kubernetes and queueing of the events.

When you want to create a controller for your (or any) entity,
read the following instructions.

When you have controllers, they are automatically added to the
DI system via their <xref:KubeOps.Operator.Controller.IResourceController`1> interface.

Controllers are registered as **scoped** elements in the DI system.
Which means, they basically behave like asp.net api controllers.
You can use dependency injection with all types of dependencies.

## Controller instance

After you created a custom entity (like described in [Entities](./entities.md))
or you want to reconcile a given entity (from the `k8s.Models` namespace,
e.g. `V1ConfigMap`) you need to create a controller class
as you would do for a MVC or API controller in asp.net.

Make sure you implement the <xref:KubeOps.Operator.Controller.IResourceController`1> interface.

```csharp
[EntityRbac(typeof(MyCustomEntity), Verbs = RbacVerb.All)]
public class FooCtrl : IResourceController<MyCustomEntity>
{
    // Implement the needed methods here.
    // The interface provides default implementation which do a NOOP.
    // Possible overwrites:
    // "Created" (i.e. when the operator sees the entity for the first time),
    // "Updated" (i.e. when the operator knows the entity and it was updated),
    // "NotModified" (i.e. when nothing changed but a timed requeue happend),
    // "StatusModified" (i.e. when only the status was updated),
    // "Deleted" (i.e. when the entity was deleted and all finalizers are done)
}
```

## Namespaced controller

To limit the operator (and therefore all controllers) to a specific
namespace in kubernetes, use the @"KubeOps.Operator.OperatorSettings"
and configure a specific namespace when it is predefined.

To use namespacing dynamically, run the application with the `--namespaced`
option. When given a name (i.e. `--namespaced=foobar`) the defined
namespace is used. When only the option is provided (i.e. `--namespaced`)
then the actual namespace is used that the pod runs in.

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

The controller's methods have a return value of <xref:KubeOps.Operator.Controller.Results.ResourceControllerResult>.
There are multiple ways how a result of a controller can be created:

- `null`: The controller will not requeue your entity / event.
- <xref:KubeOps.Operator.Controller.Results.ResourceControllerResult.RequeueEvent(System.TimeSpan)>:
  Return a result object with a <xref:System.TimeSpan> that will requeue
  the event and the entity after the time has passed.

The requeue mechanism can be useful if you want to periodically check for a database
connection for example and update the status of a given entity.

```csharp
/* snip... */
public Task<ResourceControllerResult> CreatedAsync(V1TestEntity resource)
{
    return Task.FromResult(ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(15)); // This will requeue the event in 15 seconds.
}

public Task<ResourceControllerResult> CreatedAsync(V1TestEntity resource)
{
    return Task.FromResult<ResourceControllerResult>(null); // This wont trigger a requeue.
}
/* snip... */
```

## Error requeue

If the function throws an error, the event is requeued with an exponential backoff.

```csharp
/* snip... */
public Task<ResourceControllerResult> CreatedAsync(V1TestEntity resource)
    // do something useful.
    throw new Exception("¯\\_(ツ)_/¯");
}
/* snip... */
```

Each event that errors will be retried **four times**.
