# KubeOps.Operator.Web

[![Nuget](https://img.shields.io/nuget/vpre/KubeOps.Operator.Web?label=nuget%20prerelease)](https://www.nuget.org/packages/KubeOps.Operator.Web/absoluteLatest)

This package integrates KubeOps with ASP.NET Core, enabling your operator to host the HTTP endpoints required for Kubernetes [Admission Webhooks](https://kubernetes.io/docs/reference/access-authn-authz/extensible-admission-controllers/) and [CRD Conversion Webhooks](https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definition-versioning/#configure-a-conversion-webhook).

When the Kubernetes API server needs to validate, mutate, or convert a resource as configured in a `ValidatingWebhookConfiguration`, `MutatingWebhookConfiguration`, or `CustomResourceDefinition`, it sends an HTTP request to a service endpoint. This package provides the necessary infrastructure to receive and handle these requests within your .NET operator.

> For a comprehensive explanation of different webhook types, how to implement their logic using KubeOps base classes/interfaces, and how to configure Kubernetes resources to use them, please refer to the main **[Webhooks Documentation](../../../docs/webhooks.md)**.

## Setup

Using this package requires structuring your operator as an ASP.NET Core web application:

1.  **Project SDK:** Ensure your project file (`.csproj`) uses the `Microsoft.NET.Sdk.Web` SDK:
    ```xml
    <Project Sdk="Microsoft.NET.Sdk.Web">
      ...
    </Project>
    ```
2.  **Program.cs Configuration:** Modify your `Program.cs` to configure both KubeOps and ASP.NET Core routing:

    ```csharp
    using KubeOps.Operator;

    var builder = WebApplication.CreateBuilder(args);

    // 1. Add KubeOps services and register components (controllers, finalizers, webhooks)
    builder.Services
        .AddKubernetesOperator()
        .RegisterComponents(); // Scans assemblies for controllers, finalizers, webhooks

    // Alternatively, register webhooks explicitly:
    // builder.Services.AddWebhook<MyValidationWebhook>();
    // builder.Services.AddWebhook<MyMutationWebhook>();
    // builder.Services.AddWebhook<MyConversionWebhook>();

    // 2. Add standard ASP.NET Core MVC services
    builder.Services.AddControllers();

    var app = builder.Build();

    // 3. Enable ASP.NET Core routing
    app.UseRouting();

    // 4. Map controllers (including webhook endpoints)
    app.MapControllers();

    // 5. Run the operator and web host
    await app.RunAsync();
    ```

Key points:
*   `AddKubernetesOperator()`: Initializes core KubeOps services.
*   `RegisterComponents()`: Automatically discovers and registers your implementations of controllers, finalizers, and webhooks based on their attributes and base classes.
You can also use `AddWebhook<T>()` for explicit registration if needed.
*   `AddControllers()`: Adds services required for MVC controllers, which KubeOps webhooks are built upon.
*   `MapControllers()`: Configures the ASP.NET Core routing middleware to direct incoming HTTP requests to the correct webhook controller actions.

## Webhook Endpoint Routing

KubeOps automatically configures routes for your webhooks based on the attributes you apply:

*   `[ValidationWebhook(typeof(TEntity))]`: Creates an endpoint at `/validate/{webhook-name}`.
*   `[MutationWebhook(typeof(TEntity))]`: Creates an endpoint at `/mutate/{webhook-name}`.
*   `[ConversionWebhook(typeof(TStorageEntity))]`: Creates an endpoint at `/convert/{webhook-name}`.

The `{webhook-name}` is typically derived from the C# class name of your webhook implementation (e.g., `TestValidationWebhook` might map to `/validate/testvalidationwebhook`). These are the endpoints you configure in your `ValidatingWebhookConfiguration`, `MutatingWebhookConfiguration`, or `CustomResourceDefinition` Kubernetes manifests.

## Implementation Overview

(Refer to the main [Webhooks Documentation](../../../docs/webhooks.md) for full details and examples)

*   **Validation Hooks:** Inherit from `ValidationWebhook<TEntity>` and decorate with `[ValidationWebhook(typeof(TEntity))]`.
*   **Mutation Hooks:** Inherit from `MutationWebhook<TEntity>` and decorate with `[MutationWebhook(typeof(TEntity))]`.
*   **Conversion Hooks:** Inherit from `ConversionWebhook<TStorageEntity>` and decorate with `[ConversionWebhook(typeof(TStorageEntity))]`. Implement `IEntityConverter<,>` for conversions.

## Important Considerations

*   **TLS:** Kubernetes requires webhook endpoints to be served over HTTPS. Managing TLS certificates and configuring the API server to trust them is crucial. See the [main webhook docs](../../../docs/webhooks.md#important-considerations) for more details.
*   **Dependencies:** This package brings in ASP.NET Core dependencies.

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
public class TestConversionWebhook : ConversionWebhook<V2TestEntity>
{
    protected override IEnumerable<IEntityConverter<V2TestEntity>> Converters => new IEntityConverter<V2TestEntity>[]
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

## Webhook Development

The Operator Web package can be configured to generate self-signed certificates on startup,
and create/update your webhooks in the Kubernetes cluster to point to your development
machine. To use this feature, use the `CertificateGenerator` class and `UseCertificateProvider()`
operator builder extension method. An example of what this might look like in Main:

```csharp
var builder = WebApplication.CreateBuilder(args);
string ip = "192.168.1.100";
ushort port = 443;

using CertificateGenerator generator = new CertificateGenerator(ip);
using X509Certificate2 cert = generator.Server.CopyServerCertWithPrivateKey();
// Configure Kestrel to listen on IPv4, use port 443, and use the server certificate
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(System.Net.IPAddress.Any, port, listenOptions =>
    {
        listenOptions.UseHttps(cert);
    });
});
 builder.Services
     .AddKubernetesOperator()
     // Create the development webhook service using the cert provider
     .UseCertificateProvider(port, ip, generator)
     // More code for generation, controllers, etc.
```

The `UseCertificateProvider` method takes an `ICertificateProvider` interface, so it can be used
to implement your own certificate generator/loader for development if necessary.
