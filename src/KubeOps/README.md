# KubeOps - Kubernetes Operator SDK

This package (sadly "DotnetOperatorSdk" is already taken on nuget, so its "KubeOps")
is a kubernetes operator sdk written in dotnet. It is heavily inspired by
["kubebuilder"](https://github.com/kubernetes-sigs/kubebuilder)
that provides the same and more functions for kubernetes operators in GoLang.

## Getting Started

This document should describe what steps you need to follow, to fire up your own operator.
This covers the basic installation of the operator sdk, further
clarification / documentation is in the specific sections.

The operator sdk is designed as an extension to the Generic Web Host of Microsoft.
So you'll find method extensions for `IServiceCollection` and `IApplicationBuilder`
that activate and start the operator as a web application.

### Terminology

- `Entity`: A (C#) model - an entity - that is used in kubernetes.
  An entity is the class for a kubernetes resource.
- `Resource` (or `TResource`): The type of a kubernetes resource.
- `Controller` or `ResourceController`: An instance of a resource manager
  that is responsible for the reconciliation of an entity.
- `Finalizer`: A special resource manager that is attached to the entity
  via identifier. The finalizers are called when an entity is deleted
  on kubernetes.
- `Validator`: An implementation for a validation admission webhook.
- `CRD`: CustomResourceDefinition of kubernetes.

### How To Start

Using this sdk is pretty simple:

- Create a new asp.net core application
- Install the package
- Replace the `Run` function in `Program.cs`
- Add the operator to `Startup.cs`
- Write entities / controllers / finalizers
- Go.

> If you don't create an asp.net core application (template)
> please note that the output type of the application must be an "exe":
> `<OutputType>Exe</OutputType>`

#### Install the package

```bash
dotnet add package KubeOps
```

That's it.

#### Update Entrypoint

In your `Program.cs` file, replace `Build().Run()` with `Build().RunOperatorAsync(args)`:

```csharp
public static class Program
{
    public static Task<int> Main(string[] args) =>
        CreateHostBuilder(args)
            .Build()
            .RunOperatorAsync(args);

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
```

This adds the default commands (like run and the code generators) to your app.
The commands are documentated under the [CLI Commands](./commands.md) section.

> Technically you don't need to replace the function,
> but if you don't, the other commands like yaml generation
> are not available to your application. Also namespacing is not
> possible via run flag.

#### Add to Startup.cs

```csharp
public class Startup
{
    /* snip... */
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddKubernetesOperator(); // config / settings here

        // your own dependencies
        services.AddTransient<IManager, TestManager.TestManager>();
    }

    public void Configure(IApplicationBuilder app)
    {
        // fire up the mappings for the operator
        // this is technically not needed, but if you don't call this
        // function, the healthchecks and mappings are not
        // mapped to endpoints (therefore not callable)
        app.UseKubernetesOperator();
    }
}
```

## Features

As of now, the operator sdk supports - roughly - the following features:

- Entities
  - Normal entities
  - Multi version entities
- Controller with all operations of an entity
  - Reconcile
  - StatusModified
  - Deleted
- Finalizers for entities
- Webhooks
  - Validation / validators
- Prometheus metrics for queues / caches / watchers
- Healthchecks, split up to "readiness" and "liveness" (or both)
- Commands for the operator (for exact documentation run: `dotnet run -- --help`)
  - `Run`: Start the operator and run the asp.net application
  - `Install`: Install the found CRD's into the actual configured
    cluster in your kubeconfig
  - `Uninstall`: Remove the CRDs from your cluster
  - `Generate CRD`: Generate the yaml for your CRDs
  - `Generate Docker`: Generate a dockerfile for your operator
  - `Generate Installer`: Generate a kustomization yaml for your operator
  - `Generate Operator`: Generate the yaml for your operator (rbac / role / etc)
  - `Generate RBAC`: Generate rbac roles for your CRDs

Other features and ideas are listed in the repository's
["issues"](https://github.com/buehler/dotnet-operator-sdk/issues).

## Settings

To configure the operator, use the `OperatorSettings` instance
that is configurable during the generic host extension method
`AddKubernetesOperator`.

You can configure things like the name of the operator,
if it should use namespacing, and other elements like the
urls of metrics and lease durations for the leader election.

All settings are well documented in the code docs.

## Custom Entities

The words `entity` and `resource` are kind of interchangeable. It strongly
depends on the context. The resource is the type of an object in kubernetes
which is defined by the default api or a CRD. While an entity is a class
in C# of such a resource. (CRD means "custom resource definition").

To write your own kubernetes entities, use the interfaces
provided by `k8s` or use the `CustomKubernetesEntity`.
There are two overloads with generics for the `Spec` and `Status` resource values.

A "normal" entity does not provide any real value (i.e. most of the time).
Normally you need some kind of `Spec` to have data in your entity.

The status is a subresource which can be updated without updating the
whole resource and is a flat key-value list (or should be)
of properties to represent the state of a resource.

### Write Entities

A custom entity could be:

```csharp
class FooSpec
{
    public string? Test { get; set; }
}

[KubernetesEntity(Group = "test", ApiVersion = "v1")]
public class Foo : CustomKubernetesEntity<FooSpec>
{
}
```

Now a CRD for your "Foo" class is generated on build
or via the cli commands.

If you don't use the `CustomKubernetesEntity` base class, you need to - at least - use the appropriate interfaces from `k8s`:

- `KubernetesObject`
- `IKubernetesObject<V1ObjectMeta>`

#### Ignoring Entities

There are use-cases when you want to model / watch a custom entity from another
software engineer that are not part of the base models in `k8s`.

To prevent the generator from creating yaml's for CRDs you don't own, use
the `IgnoreEntityAttribute`.

So as an example, one could try to watch for Ambassador-Mappings with
the following entity:

```csharp
public class MappingSpec
{
    public string Host { get; set; }
}

[IgnoreEntity]
[KubernetesEntity(Group = "getambassador.io", ApiVersion = "v2")]
public class Mapping : CustomKubernetesEntity<MappingSpec>
{
}
```

You need it to be a `KubernetesEntity` and a `IKubernetesObject<V1ObjectMeta>`, but
you don't want a CRD generated for it (thus the `IgnoreEntity` attribute).

### RBAC

The operator (SDK) will generate the role config for your
operator to be installed. When your operator needs access to
Kubernetes objects, they must be mentioned with the
RBAC attributes. During build, the SDK scans the configured
types and generates the RBAC role that the operator needs
to function.

There exist two versions of the attribute:
`KubeOps.Operator.Rbac.EntityRbacAttribute` and
`KubeOps.Operator.Rbac.GenericRbacAttribute`.

The generic RBAC attribute will be translated into a `V1PolicyRole`
according to the properties set in the attribute.

```csharp
[GenericRbac(Groups = new []{"apps"}, Resources = new[]{"deployments"}, Verbs = RbacVerb.All)]
```

The entity RBAC attribute is the elegant option to use
dotnet mechanisms. The CRD information is generated out of
the given types and then grouped by type and used RBAC verbs.
If you create multiple attributes with the same type, they are
concatenated.

```csharp
[EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Get | RbacVerb.Update)]
```

### Validation

During CRD generation, the generated json schema uses the types
of the properties to create the openApi schema.

You can use the various validator attributes to customize your crd:

(all attributes are on properties with the exception of the Description)

- `Description`: Describe the property or class
- `ExternalDocs`: Add a link to an external documentation
- `Items`: Customize MinItems / MaxItems and if the items should be unique
- `Length`: Customize the length of something
- `MultipleOf`: A number should be a multiple of
- `Pattern`: A valid ECMA script regex (e.g. `/\d*/`)
- `RangeMaximum`: The maximum of a value (with option to exclude the max itself)
- `RangeMinimum`: The minimum of a value (with option to exclude the min itself)
- `Required`: The field is listed in the required fields
- `PreserveUnknownFields`: Set the `X-Kubernetes-Preserve-Unknown-Fields` to `true`

> For `Description`: if your project generates the XML documentation files
> for the result, the crd generator also searches for those files and a possible
> `<summary>` tag in the xml documentation. The attribute will take precedence though.

```csharp
public class MappingSpec
{
    /// <summary>This is a comment.</summary>
    [Description("This is another comment")]
    public string Host { get; set; }
}
```

In the example above, the text of the attribute will be used.

### Multi-Version Entities

You can manage multiple versions of a CRD. To do this, you can
specify multiple classes as the "same" entity, but with different
versions.

To mark multiple entity classes as the same, use exactly the same
`Kind`, `Group` and `PluralName` and differ in the `ApiVersion`
field.

#### Version priority

Sorting of the versions - and therefore determine which version should be
the `storage version` if no attribute is provided - is done by the kubernetes
rules of version sorting:

Priority is as follows:

1. General Availablility (i.e. `V1Foobar`, `V2Foobar`)
2. Beta Versions (i.e. `V11Beta13Foobar`, `V2Beta1Foobar`)
3. Alpha Versions (i.e. `V16Alpha13Foobar`, `V2Alpha10Foobar`)

The parsed version numbers are sorted by the highest first, this leads
to the following version priority:

```
- v10
- v2
- v1
- v11beta2
- v10beta3
- v3beta1
- v12alpha1
- v11alpha2
```

This can also be reviewed in the
[Kubernetes documentation](https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definition-versioning/#version-priority).

#### Storage Version

To determine the storage version (of which one, and exactly one must exist)
the system uses the previously mentioned version priority to sort the versions
and picking the first one. To overwrite this behaviour, use the
`KubeOps.Operator.Entities.Annotations.StorageVersionAttribute`

> When multiple `KubeOps.Operator.Entities.Annotations.StorageVersionAttribute`
> are used, the system will thrown an error.

To overwrite a version, annotate the entity class with the attribute.

#### Example

##### Normal multiversion entity

Note that the `Kind`

```csharp
[KubernetesEntity(
    ApiVersion = "v1",
    Kind = "VersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "versionedentities")]
public class V1VersionedEntity : CustomKubernetesEntity
{
}

[KubernetesEntity(
    ApiVersion = "v1beta1",
    Kind = "VersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "versionedentities")]
public class V1Beta1VersionedEntity : CustomKubernetesEntity
{
}

[KubernetesEntity(
    ApiVersion = "v1alpha1",
    Kind = "VersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "versionedentities")]
public class V1Alpha1VersionedEntity : CustomKubernetesEntity
{
}
```

The resulting storage version would be `V1VersionedEntity`.

##### Overwritten storage version multi-version entity

```csharp
[KubernetesEntity(
    ApiVersion = "v1",
    Kind = "AttributeVersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "attributeversionedentities")]
[StorageVersion]
public class V1AttributeVersionedEntity : CustomKubernetesEntity
{
}

[KubernetesEntity(
    ApiVersion = "v2",
    Kind = "AttributeVersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "attributeversionedentities")]
public class V2AttributeVersionedEntity : CustomKubernetesEntity
{
}
```

The resulting storage version would be `V1AttributeVersionedEntity`.

## Resource Controller

When reconciling an entity of a `CRD`, one needs a controller to do so.
The controller abstracts the general complexity of watching the
resources on kubernetes and queueing of the events.

When you want to create a controller for your (or any) entity,
read the following instructions.

When you have controllers, they are automatically added to the
DI system via their `KubeOps.Operator.Controller.IResourceController` interface.

Controllers are registered as **scoped** elements in the DI system.
Which means, they basically behave like asp.net api controllers.
You can use dependency injection with all types of dependencies.

### Controller instance

After you created a custom entity (like described in [Entities](#custom-entities))
or you want to reconcile a given entity (from the `k8s.Models` namespace,
e.g. `V1ConfigMap`) you need to create a controller class
as you would do for a MVC or API controller in asp.net.

Make sure you implement the `KubeOps.Operator.Controller.IResourceController` interface.

```csharp
[EntityRbac(typeof(MyCustomEntity), Verbs = RbacVerb.All)]
public class FooCtrl : IResourceController<MyCustomEntity>
{
    // Implement the needed methods here.
    // The interface provides default implementation which do a NOOP.
    // Possible overwrites:
    // "ReconcileAsync": when the operator sees the entity for the first time, it was modified or just fired an event,
    // "StatusModifiedAsync" (i.e. when only the status was updated),
    // "DeletedAsync" (i.e. when the entity was deleted and all finalizers are done)
}
```

### Namespaced controller

To limit the operator (and therefore all controllers) to a specific
namespace in kubernetes, use the `KubeOps.Operator.OperatorSettings`
and configure a specific namespace when it is predefined.

To use namespacing dynamically, run the application with the `--namespaced`
option. When given a name (i.e. `--namespaced=foobar`) the defined
namespace is used. When only the option is provided (i.e. `--namespaced`)
then the actual namespace is used that the pod runs in.

### RBAC

The entity rbac attribute does provide the information needed about
your needed roles / rules.

Please configure all entities you want to manage with your
operator with such an entity rbac attribute. This generates
the rbac roles / role bindings for your operator and therefore
for the service account associated with the operator.

#### EntityRbac

The first possibility to configure rbac is with the `KubeOps.Operator.Rbac.EntityRbacAttribute`
attribute.

The attribute takes a list of types (your entities) and a `KubeOps.Operator.Rbac.RbacVerb`.
The verbs define the needed permissions and roles for the given entity(ies).

You can configure multiple types and even well known entities from kubernetes:

```csharp
[EntityRbac(typeof(MyCustomEntity), Verbs = RbacVerb.All)]
[EntityRbac(typeof(V1Secret), typeof(V1ConfigMap), Verbs = RbacVerb.Get | RbacVerb.List)]
[EntityRbac(typeof(V1Deployment), Verbs = RbacVerb.Create | RbacVerb.Update | RbacVerb.Delete)]
```

#### GenericRbac

The second possibility is to use the `KubeOps.Operator.Rbac.GenericRbacAttribute`
which takes a list of api groups, resources, versions and a selection of
RbacVerbs to configure the rbac rule:

```csharp
[GenericRbac(Groups = new {"apps"}, Resources = new {"deployments"}, Verbs = RbacVerb.All)]
```

### Requeue

The controller's methods (reconcile) have
a return value of `KubeOps.Operator.Controller.Results.ResourceControllerResult`.
There are multiple ways how a result of a controller can be created:

- `null`: The controller will not requeue your entity / event.
- `KubeOps.Operator.Controller.Results.ResourceControllerResult.RequeueEvent`:
  Return a result object with a `System.TimeSpan` that will requeue
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

### Error requeue

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

## Events / Event Series

Kubernetes knows "Events" which can be sort of attached to a resource
(i.e. a Kubernetes object).

To create and use events, inject the @"KubeOps.Operator.Events.IEventManager"
into your controller. It is registered as a transient resource in the DI
container.

### IEventManager

#### Publish events

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
await manager.PublishAsync(resource, "reason", "my fancy message");
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
await manager.PublishAsync(@event);
```

#### Use publisher delegates

If you don't want to call the `KubeOps.Operator.Events.IEventManager.PublishAsync`
all the time with the same arguments, you can create delegates.

There exist two different delegates:
- "AsyncStaticPublisher": Predefined event
  on a predefined resource.
- "AsyncPublisher": Predefined event
  on a variable resource.

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
public class TestController : IResourceController<V1TestEntity>
{
    private readonly IEventManager.Publisher _publisher;

    public TestController(IEventManager eventManager)
    {
        _publisher = eventManager.CreatePublisher("reason", "my fancy message");
    }

    public Task<ResourceControllerResult> CreatedAsync(V1TestEntity resource)
    {
        // Here, the event is published with predefined strings
        // but for a "variable" resource.
        await _publisher(resource);
        return Task.FromResult<ResourceControllerResult>(null);
    }
}
```

## Finalizers

A finalizer is a special type of software that can asynchronously
cleanup stuff for an entity that is being deleted.

A finalizer is registered as an identifier in a kubernetes
object (i.e. in the yaml / json structure) and the object
wont be removed from the api until all finalizers are removed.

If you write finalizer, they will be automatically added to the
DI system via their type `KubeOps.Operator.Finalizer.IResourceFinalizer`

### Write a finalizer

Use the correct interface (`KubeOps.Operator.Finalizer.IResourceFinalizer`).

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

The interface also provides a way of overwriting the `Identifier` of the finalizer if you feed like it.

When the finalizer successfully completed his job, it is automatically removed
from the finalizers list of the entity. The finalizers are registered
as scoped resources in DI.

### Register a finalizer

To attach a finalizer for a resource, call the
`KubeOps.Operator.Finalizer.IFinalizerManager.RegisterFinalizerAsync`
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

Alternatively, the `KubeOps.Operator.Finalizer.IFinalizerManager.RegisterAllFinalizersAsync`
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

### Unregistering a finalizer

When a resource is finalized, the finalizer is removed automatically.
However, if you want to remove a finalizer before a resource is deleted/finalized,
you can use `KubeOps.Operator.Finalizer.IFinalizerManager.RemoveFinalizerAsync`.

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

## Webhooks

Kubernetes supports various webhooks to extend the normal api behaviour
of the master api. Those are documented on the
[kubernetes website](https://kubernetes.io/docs/reference/access-authn-authz/extensible-admission-controllers/).

`KubeOps` supports the following webhooks out of the box:

- Validator / Validation
- Mutator / Mutation

The following documentation should give the user an overview
on how to implement a webhook what this implies to the written operator.

At the courtesy of the kubernetes website, here is a diagram of the
process that runs for admission controllers and api requests:

![admission controller phases](https://d33wubrfki0l68.cloudfront.net/af21ecd38ec67b3d81c1b762221b4ac777fcf02d/7c60e/images/blog/2019-03-21-a-guide-to-kubernetes-admission-controllers/admission-controller-phases.png)

### General

In general, if your operator contains _any_ registered (registered in the
DI) the build process that is provided via `KubeOps.targets` will
generate a CA certificate for you.

So if you add a webhook to your operator the following changes
to the normal deployment of the operator will happen:

1. During "after build" phase, the sdk will generate
   a CA-certificate for self signed certificates for you.
2. The ca certificate and the corresponding key are added
   to the deployment via kustomization config.
3. A special config is added to the deployment via
   kustomization to use https.
4. The deployment of the operator now contains an `init-container`
   that loads the `ca.pem` and `ca-key.pem` files and creates
   a server certificate. Also, a service and the corresponding
   webhook configurations are created.
5. When the operator starts, an additional https route is registered
   with the created server certificate.

When a webhook is registered, the specified operations will
trigger a POST call to the operator.

> The certificates are generated with [cfssl](https://github.com/cloudflare/cfssl),
> an amazing tool from cloudflare that helps with the general hassle
> of creating CAs and certificates in general.

> Make sure you commit the `ca.pem` / `ca-key.pem` file.
> During operator startup (init container) those files
> are needed. Since this represents a self signed certificate,
> and it is only used for cluster internal communication,
> it is no security issue to the system. The service is not
> exposed to the internet.

> The `server.pem` and `server-key.pem` files are generated
> in the init container during pod startup.
> Each pod / instance of the operator gets its own server
> certificate but the CA must be shared among them.

### Local development

It is possible to test / debug webhooks locally. For this, you need
to implement the webhook and use assembly-scanning (or the
operator builder if you disabled scanning) to register
the webhook type.

There are two possibilities to tell Kubernetes that it should
call your local running operator for the webhooks. The url
that Kubernetes addresses _must_ be an HTTPS address.

#### Using `AddWebhookLocaltunnel`

In your `Startup.cs` you can use the `IOperatorBuilder`
method `AddWebhookLocaltunnel` to add an automatic
localtunnel instance to your operator.

This will cause the operator to register a hosted service that
creates a tunnel and then registers itself to Kubernetes
with the created proxy-url. Now all calls are automatically
forwarded via HTTPS to your operator.

```csharp
namespace KubeOps.TestOperator
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
               .AddKubernetesOperator()
#if DEBUG
               .AddWebhookLocaltunnel()
#endif
               ;
            services.AddTransient<IManager, TestManager.TestManager>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseKubernetesOperator();
        }
    }
}
```

> It is _strongly_ advices against using auto-webhooks
> with localtunnel in production. This feature
> is intended to improve the developer experience
> while coding operators.

> Some IDEs (like Rider from JetBrains) do not correctly
> terminate debugged applications. Hence, the
> webhook registration remains in Kubernetes. If you remove
> webhooks from your operator, you need to remove the
> registration within Kubernetes as well.

#### Using external proxy

The operator will run on a specific http address, depending on your
configuration.
Now, use [ngrok](https://ngrok.com/) or
[localtunnel](https://localtunnel.github.io/www/) or something
similar to create a HTTPS tunnel to your local running operator.

Now you can use the cli command of the sdk
`dotnet run -- webhooks register --base-url <<TUNNEL URL>>` to
register the webhooks under the tunnel's url.

The result is your webhook being called by the kubernetes api.
It is suggested one uses `Docker Desktop` with kubernetes.

### Validation webhook

The general idea of this webhook type is to validate an entity
before it is definitely created / updated or deleted.

Webhooks are registered in a **scoped** manner to the DI system.
They behave like asp.net api controller.

The implementation of a validator is fairly simple:

- Create a class somewhere in your project.
- Implement the @"KubeOps.Operator.Webhooks.IValidationWebhook`1" interface.
- Define the @"KubeOps.Operator.Webhooks.IAdmissionWebhook`2.Operations"
  (from the interface) that the validator is interested in.
- Overwrite the corresponding methods.

> The interface contains default implementations for _ALL_ methods.
> The default of the async methods are to call the sync ones.
> The default of the sync methods is to return a "not implemented"
> result.
> The async methods take precedence over the synchronous ones.

The return value of the validation methods are
@"KubeOps.Operator.Webhooks.ValidationResult"
objects. A result contains a boolean flag if the entity / operation
is valid or not. It may contain additional warnings (if it is valid)
that are presented to the user if the kubernetes api supports it.
If the result is invalid, one may add a custom http status code
as well as a custom error message that is presented to the user.

#### Example

```c#
public class TestValidator : IValidationWebhook<EntityClass>
 {
     public AdmissionOperations Operations => AdmissionOperations.Create | AdmissionOperations.Update;

     public ValidationResult Create(EntityClass newEntity, bool dryRun) =>
         CheckSpec(newEntity)
             ? ValidationResult.Success("The username may not be foobar.")
             : ValidationResult.Fail(StatusCodes.Status400BadRequest, @"Username is ""foobar"".");

     public ValidationResult Update(EntityClass _, EntityClass newEntity, bool dryRun) =>
         CheckSpec(newEntity)
             ? ValidationResult.Success("The username may not be foobar.")
             : ValidationResult.Fail(StatusCodes.Status400BadRequest, @"Username is ""foobar"".");

     private static bool CheckSpec(EntityClass entity) => entity.Spec.Username != "foobar";
 }
```

### Mutation webhook

Mutators are similar to validators but instead of defining if an object is
valid or not, they are able to modify an object on the fly. The result
of a mutator may generate a JSON Patch (http://jsonpatch.com) that patches
the object that is later passed to the validators and to the Kubernetes
API.

The implementation of a mutator is fairly simple:

- Create a class somewhere in your project.
- Implement the "KubeOps.Operator.Webhooks.IMutationWebhook" interface.
- Define the "KubeOps.Operator.Webhooks.IAdmissionWebhook.Operations"
  (from the interface) that the validator is interested in.
- Overwrite the corresponding methods.

> The interface contains default implementations for _ALL_ methods.
> The default of the async methods are to call the sync ones.
> The default of the sync methods is to return a "not implemented"
> result.
> The async methods take precedence over the synchronous ones.

The return value of the mutation methods do indicate if
there has been a change in the model or not. If there is no
change, return a result from "KubeOps.Operator.Webhooks.MutationResult.NoChanges"
and if there are changes, modify the object that is passed to the
method and return the changed object with
"KubeOps.Operator.Webhooks.MutationResult.Modified(System.Object)".
The system then calculates the diff and creates a JSON patch for
the object.

## Operator utils

There are two basic utilities that should be mentioned:

- Health-checks
- Metrics

### Healthchecks

This is a basic feature of asp.net. The operator sdk makes use of
it and splits them up into `Liveness` and `Readiness` checks.

With the appropriate methods, you can add an `IHealthCheck` interface
to either `/ready`, `/health` or both.

The urls can be configured via "KubeOps.Operator.OperatorSettings".

- "AddHealthCheck":
  adds a healthcheck to ready and liveness
- "AddLivenessCheck":
  adds a healthcheck to the liveness route only
- "AddReadinessCheck":
  adds a healthcheck to the readiness route only

### Metrics

By default, the operator lists some interessting metrics on the
`/metrics` route. The url can be configured via @"KubeOps.Operator.OperatorSettings".

There are many counters on how many elements have been reconciled, if the
controllers and queues are up and how many elements are in timed requeue state.

Please have a look at the metrics if you run your operator locally or online
to see which metrics are available.

Of course you can also have a look at the used metrics classes to see the
implementation: [Metrics Implementations](https://github.com/buehler/dotnet-operator-sdk/tree/master/src/KubeOps/Operator/DevOps).

## Entity / Resource utils

There are several method extensions that help with day to day resource
handling. Head over to their documentation to see that they do:

- `KubeOps.Operator.Entities.Extensions.KubernetesObjectExtensions.MakeObjectReference`
- `KubeOps.Operator.Entities.Extensions.KubernetesObjectExtensions.MakeOwnerReference`
- `KubeOps.Operator.Entities.Extensions.KubernetesObjectExtensions.WithOwnerReference`

## Commands

For convenience, there are multiple commands added to the executable
of your operator (through the KubeOps package).

Those are implemented with the [CommandLineUtils by NateMcMaster](https://github.com/natemcmaster/CommandLineUtils).

you can see the help and overview when using
`dotnet run -- --help` in your project. As you can see, you can run
multiple commands. Some of them do install / uninstall your crds in
your currently selected kubernetes cluster or can generate code.

> For the normal "dotnet run" command exists a `--namespaced`
> option that starts the operator in namespaced mode. This means
> that only the given namespace is watched for entities.

### Available Commands

Here is a brief overview over the available commands:

> all commands assume either the compiled dll or you using
> `dotnet run -- ` as prepended command.

- `""` (empty): runs the operator (normal `dotnet run`)
- `version`: prints the version information for the actual connected kubernetes cluster
- `install`: install the CRDs for the solution into the cluster
- `uninstall`: uninstall the CRDs for the solution from the cluster
- `generator`: entry command for generator commands (i.e. has subcommands), all commands
  output their result to the stdout or the given output path
  - `crd`: generate the CRDs
  - `docker`: generate the dockerfile
  - `installer`: generate the installer files (i.e. kustomization yaml) for the operator
  - `operator`: generate the deployment for the operator
  - `rbac`: generate the needed rbac roles / role bindings for the operator
- `webhook`: entry command for webhook related operations
  - `install`: generate the server certificate and install the service / webhook registration
  - `register`: register the currently implemented webhooks to the currently selected cluster

### Code Generation

When installing this package, you also reference the default Targets and Props
that come with the build engine. While building the following elements are generated:

- Dockerfile (if not already present)
- CRDs for your custom entities
- RBAC roles and role bindings for your requested resources
- Deployment files for your operator
- Installation file for your operator (kustomize)

The dockerfile will not be overwritten in case you have custom elements in there.
The installation files won't be overwritten as well if you have custom elements in there.

To regenerate those two elements, just delete them and rebuild your code.

For the customization on those build targets, have a look at the next section.

## MS Build extensions

This project extends the default build process of dotnet with some
code generation targets after the build.

You'll find the configurations and targets here:

- [KubeOps.targets](https://github.com/buehler/dotnet-operator-sdk/blob/master/src/KubeOps/Build/KubeOps.targets): defines the additional build targets

They can be configured with the prop settings described below.
The props file just defines the defaults.

### Prop Settings

You can overwrite the default behaviour of the building parts with the following
variables that you can add in a `<PropertyGroup>` in your `csproj` file:

| Property               | Description                                                                | Default Value                                                           |
| ---------------------- | -------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| KubeOpsBasePath        | Base path for all other elements                                           | `$(MSBuildProjectDirectory)`                                            |
| KubeOpsDockerfilePath  | The path of the dockerfile                                                 | `$(KubeOpsBasePath)\Dockerfile`                                         |
| KubeOpsDockerTag       | Which dotnet sdk / run tag should be used                                  | `latest`                                                                |
| KubeOpsConfigRoot      | The base directory for generated elements                                  | `$(KubeOpsBasePath)\config`                                             |
| KubeOpsCrdDir          | The directory for the generated crds                                       | `$(KubeOpsConfigRoot)\crds`                                             |
| KubeOpsCrdFormat       | Output format for crds                                                     | `Yaml`                                                                  |
| KubeOpsCrdUseOldCrds   | Use V1Beta version of crd instead of V1<br>(for kubernetes version < 1.16) | `false`                                                                 |
| KubeOpsRbacDir         | Where to put the roles                                                     | `$(KubeOpsConfigRoot)\rbac`                                             |
| KubeOpsRbacFormat      | Output format for rbac                                                     | `Yaml`                                                                  |
| KubeOpsOperatorDir     | Where to put operator related elements<br>(e.g. Deployment)                | `$(KubeOpsConfigRoot)\operator`                                         |
| KubeOpsOperatorFormat  | Output format for the operator                                             | `Yaml`                                                                  |
| KubeOpsInstallerDir    | Where to put the installation files<br>(e.g. Namespace / Kustomization)    | `$(KubeOpsConfigRoot)\install`                                          |
| KubeOpsInstallerFormat | Output format for the installation files                                   | `Yaml`                                                                  |
| KubeOpsSkipDockerfile  | Skip dockerfile during build                                               | `""`                                                                    |
| KubeOpsSkipCrds        | Skip crd generation during build                                           | `""`                                                                    |
| KubeOpsSkipRbac        | Skip rbac generation during build                                          | `""`                                                                    |
| KubeOpsSkipOperator    | Skip operator generation during build                                      | `""`                                                                    |
| KubeOpsSkipInstaller   | Skip installer generation during build                                     | `""`                                                                    |

## Advanced Topics

### Assembly Scanning

By default, KubeOps scans the assembly containing the main entrypoint for
controller, finalizer, webhook and entity types, and automatically registers
all types that implement the correct interfaces for usage.

If some of the above are stored in a different assembly, KubeOps must be
specifically instructed to scan that assembly `KubeOps.Operator.Builder.IOperatorBuilder.AddResourceAssembly` or else those types won't be loaded.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddKubernetesOperator()
            .AddResourceAssembly(typeof(CustomEntityController).Assembly)
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseKubernetesOperator();
    }
}
```

### Manual Registration

If desired, the default behavior of assembly scanning can be disabled so
specific components can be registered manually. (Using both methods in parallel
is supported, such as if you want to load all components from one assembly and
only some from another.)

See `KubeOps.Operator.Builder.IOperatorBuilder` for details on the methods
utilized in this registration pattern.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddKubernetesOperator(settings =>
            {
                settings.EnableAssemblyScanning = false;
            })
            .AddEntity<V1DemoEntityClone>()
            .AddController<DemoController, V1DemoEntityClone>()
            .AddController<DemoControllerClone>()
            .AddFinalizer<DemoFinalizer>()
            .AddValidationWebhook<DemoValidator>()
            .AddMutationWebhook<DemoMutator>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseKubernetesOperator();
    }
}
```
