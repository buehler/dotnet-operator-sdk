# KubeOps Operator Web

The KubeOps Operator Web package provides a webserver to enable
webhooks for your Kubernetes operator.

## Usage

To enable webhooks and external access to your operator, you need to
use ASP.net. The project file needs to reference `Microsoft.NET.Sdk.Web`
instead of `Microsoft.NET.Sdk` and the `Program.cs` needs to be changed.

To allow webhooks, the MVC controllers need to be registered and mapped.

The basic `Program.cs` setup looks like this:

```csharp
using KubeOps.Operator;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddKubernetesOperator()
    .RegisterComponents();

builder.Services
    .AddControllers();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

await app.RunAsync();
```

Note the `.AddControllers` and `.MapControllers` call.
Without them, your webhooks will not be reachable.

## Validation Hooks

To create a validation webhook, first create a new class
that implements the `ValidationWebhook<T>` base class.
Then decorate the webhook with the `ValidationWebhookAttribute`
to set the route correctly.

After that setup, you may overwrite any of the following methods:

- Create
- CreateAsync
- Update
- UpdateAsync
- Delete
- DeleteAsync

The async methods take precedence over the sync methods.

An example of such a validation webhook looks like:

```csharp
[ValidationWebhook(typeof(V1TestEntity))]
public class TestValidationWebhook : ValidationWebhook<V1TestEntity>
{
    public override ValidationResult Create(V1TestEntity entity, bool dryRun)
    {
        if (entity.Spec.Username == "forbidden")
        {
            return Fail("name may not be 'forbidden'.", 422);
        }

        return Success();
    }

    public override ValidationResult Update(V1TestEntity oldEntity, V1TestEntity newEntity, bool dryRun)
    {
        if (newEntity.Spec.Username == "forbidden")
        {
            return Fail("name may not be 'forbidden'.");
        }

        return Success();
    }
}
```

To create the validation results, use the `protected` methods (`Success` and `Fail`)
like "normal" `IActionResult` creation methods.

## Mutation Hooks

To create a mutation webhook, first create a new class
that implements the `MutationWebhook<T>` base class.
Then decorate the webhook with the `MutationWebhookAttribute`
to set the route correctly.

After that setup, you may overwrite any of the following methods:

- Create
- CreateAsync
- Update
- UpdateAsync
- Delete
- DeleteAsync

The async methods take precedence over the sync methods.

An example of such a mutation webhook looks like:

```csharp
[MutationWebhook(typeof(V1TestEntity))]
public class TestMutationWebhook : MutationWebhook<V1TestEntity>
{
    public override MutationResult<V1TestEntity> Create(V1TestEntity entity, bool dryRun)
    {
        if (entity.Spec.Username == "overwrite")
        {
            entity.Spec.Username = "random overwritten";
            return Modified(entity);
        }

        return NoChanges();
    }
}
```

To create the mutation results, use the `protected` methods (`NoChanges`, `Modified`, and `Fail`)
like "normal" `IActionResult` creation methods.

## Conversion Hooks

> [!CAUTION]
> Conversion webhooks are not stable yet. The API may change in the future without
> a new major version. All code related to conversion webhooks are attributed
> with the `RequiresPreviewFeatures` attribute.
> To use the features, you need to enable the preview features in your project file
> with the `<EnablePreviewFeatures>true</EnablePreviewFeatures>` property.

A conversion webhook is a special kind of webhook that allows you to convert
Kubernetes resources between versions. The webhooks are installed in CRDs
and are called for all objects that need conversion (i.e. to achieve the stored
version state).

A conversion webhook is separated to the webhook itself (the MVC controller
that registers its route within ASP.NET) and the conversion logic.

The following example has two versions of the "TestEntity" (v1 and v2) and
implements a conversion webhook to convert from v1 to v2 and vice versa.

```csharp
// The Kubernetes resources
[KubernetesEntity(Group = "webhook.dev", ApiVersion = "v1", Kind = "TestEntity")]
public partial class V1TestEntity : CustomKubernetesEntity<V1TestEntity.EntitySpec>
{
    public override string ToString() => $"Test Entity v1 ({Metadata.Name}): {Spec.Name}";

    public class EntitySpec
    {
        public string Name { get; set; } = string.Empty;
    }
}

[KubernetesEntity(Group = "webhook.dev", ApiVersion = "v2", Kind = "TestEntity")]
public partial class V2TestEntity : CustomKubernetesEntity<V2TestEntity.EntitySpec>
{
    public override string ToString() => $"Test Entity v2 ({Metadata.Name}): {Spec.Firstname} {Spec.Lastname}";

    public class EntitySpec
    {
        public string Firstname { get; set; } = string.Empty;

        public string Lastname { get; set; } = string.Empty;
    }
}
```

The v1 of the resource has first and lastname in the same field, while the v2
has them separated.

```csharp
public class V1ToV2 : IEntityConverter<V1TestEntity, V2TestEntity>
{
    public V2TestEntity Convert(V1TestEntity from)
    {
        var nameSplit = from.Spec.Name.Split(' ');
        var result = new V2TestEntity { Metadata = from.Metadata };
        result.Spec.Firstname = nameSplit[0];
        result.Spec.Lastname = string.Join(' ', nameSplit[1..]);
        return result;
    }

    public V1TestEntity Revert(V2TestEntity to)
    {
        var result = new V1TestEntity { Metadata = to.Metadata };
        result.Spec.Name = $"{to.Spec.Firstname} {to.Spec.Lastname}";
        return result;
    }
}
```

The conversion logic is implemented in the `IEntityConverter` interface.
Each converter has a "convert" (from -> to) and a "revert" (to -> from) method.

```csharp
[ConversionWebhook(typeof(V2TestEntity))]
public class TestConversionWebhook : ConversionWebhook
{
    protected override IEnumerable<IEntityConverter> Converters => new IEntityConverter[]
    {
        new V1ToV2(), // other versions...
    };
}
```

The webhook the registers the list of possible converters and
calls the converter upon request.

> [!NOTE]
> There needs to be a conversion between ALL versions to the
> stored version (newest version). If there is no conversion,
> the webhook will fail and the resource is not stored.
> So if there exist a v1, v2, and v3, there needs to be a
> converter for v1 -> v3 and v2 -> v3 (when v3 is the stored version).

## Installing In The Cluster

When creating an operator with webhooks, certain special resources must be provided
to run in the cluster. When this package is referenced and KubeOps.Cli is installed,
these resources should be generated automatically. Basically, instead of
generating a dockerfile with `dotnet:runtime` as final image, you'll need
`dotnet:aspnet` and the operator needs a service and the certificates
for the HTTPS connection since webhooks only operate over HTTPS.

With the KubeOps.Cli package you can generate the required resources or
let the customized Build targets do it for you.

The targets create a CA certificate and a server certificate (with respective keys),
a service, and the webhook registrations required for you.

> [!WARNING]
> The generated certificate has a validity of 5 years. After that time,
> the certificate needs to be renewed. For now, there is no automatic
> renewal process.
