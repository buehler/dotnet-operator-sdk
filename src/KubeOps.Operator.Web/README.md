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

TODO.

## Conversion Hooks

TODO.

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
