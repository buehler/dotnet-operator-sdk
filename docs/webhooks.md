# Webhooks

**Note:** Using KubeOps webhooks requires your operator to be hosted within an ASP.NET Core application. The `KubeOps.Operator.Web` NuGet package provides the necessary integration and middleware.

KubeOps leverages ASP.NET Core to host webhook endpoints, allowing your operator to interact with the Kubernetes API server during crucial lifecycle events. This requires the `KubeOps.Operator.Web` NuGet package.

There are two main categories of webhooks supported:

1.  **Admission Webhooks:** Intercept requests to the Kubernetes API server *before* an object is persisted (created, updated, or deleted). They can either **validate** the request or **mutate** (modify) the object.
2.  **Conversion Webhooks:** Handle the conversion of custom resources between different versions defined in your CRD.

## Admission Webhooks

[Admission Webhooks](https://kubernetes.io/docs/reference/access-authn-authz/extensible-admission-controllers/) act as gatekeepers for API requests.

### Implementation Steps

1.  **Add Package:** Ensure your operator project references `KubeOps.Operator.Web`.
2.  **Create Implementation:**
    *   For **validation**, create a class inheriting from `ValidationWebhook<TEntity>` (found in `KubeOps.Operator.Web.Webhooks.Admission.Validation`).
    *   For **mutation**, create a class inheriting from `MutationWebhook<TEntity>` (found in `KubeOps.Operator.Web.Webhooks.Admission.Mutation`).
    *   `TEntity` is the Kubernetes resource type the webhook applies to (e.g., `V1Pod`, or your own `V1MyCustomResource`).
3.  **Add Attribute:** Decorate your implementation class with `[ValidationWebhook(typeof(TEntity))]` or `[MutationWebhook(typeof(TEntity))]` respectively.
4.  **Implement Methods:** Override the relevant methods (`Create`, `Update`, `Delete`) to contain your logic.
    *   Validation methods return `ValidationResult`, typically `Success()` or `Fail("reason", [optional httpStatusCode])`.
    *   Mutation methods return `MutationResult<TEntity>`, typically `NoChanges()` or `Modified(updatedEntity)`.
5.  **Register:** In your `Program.cs` or other startup code, register the webhook implementation using the standard .NET dependency injection container:
    ```csharp
    // Example registration in Program.cs
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddKubernetesOperator(); // Base KubeOps registration

    // Register specific webhook implementations
    builder.Services.AddWebhook<MyValidationWebhook>(); 
    builder.Services.AddWebhook<MyMutationWebhook>();

    // ... other service registrations ...

    var app = builder.Build();
    app.UseKubernetesOperator(); // Enable KubeOps endpoints
    // ... other middleware ...
    app.Run();
    ```
6.  **Configure Kubernetes:** Create `ValidatingWebhookConfiguration` or `MutatingWebhookConfiguration` Kubernetes resources. These tell the API server to send admission requests for specific resources/operations to your operator's webhook service endpoint (usually `/mutate/{webhook-name}` or `/validate/{webhook-name}`).
    *   **Important:** Manually creating these YAML configurations can be complex. The KubeOps [CLI Tool](./cli.md) is **highly recommended** for generating these based on your webhook attributes: `dotnet kubeops generate webhooks --assembly /path/to/your/operator.dll --output-path ./deployment`.
    *   Additionally, the command `dotnet kubeops generate operator` (which also generates RBAC) creates the necessary Kubernetes `Service` manifest required to expose your webhook endpoints within the cluster so the API server can reach them.

### Validation Webhook Example

This example prevents creating or updating a `V1TestEntity` if its `spec.username` is "forbidden".

```csharp
// From examples/WebhookOperator/Webhooks/TestValidationWebhook.cs
using KubeOps.Operator.Web.Webhooks.Admission.Validation;
using WebhookOperator.Entities; // Assuming V1TestEntity is defined here

namespace WebhookOperator.Webhooks;

[ValidationWebhook(typeof(V1TestEntity))]
public class TestValidationWebhook : ValidationWebhook<V1TestEntity>
{
    // Only implement methods for operations you want to validate
    public override ValidationResult Create(V1TestEntity entity, bool dryRun)
    {
        if (entity.Spec.Username == "forbidden")
        {
            // Reject the request with a reason and HTTP status code 422
            return Fail("name may not be 'forbidden'.", 422);
        }
        return Success(); // Allow the request
    }

    public override ValidationResult Update(V1TestEntity oldEntity, V1TestEntity newEntity, bool dryRun)
    {
        if (newEntity.Spec.Username == "forbidden")
        {
            // Reject the request with a reason (defaults to HTTP 400)
            return Fail("name may not be 'forbidden'.");
        }
        return Success(); // Allow the request
    }
}
```

Find the full example code here: [https://github.com/ewassef/dotnet-operator-sdk/tree/main/examples/WebhookOperator/](https://github.com/ewassef/dotnet-operator-sdk/tree/main/examples/WebhookOperator/).

### Mutation Webhook Example

This example changes `spec.username` to "random overwritten" if it's initially set to "overwrite" during creation.

```csharp
// From examples/WebhookOperator/Webhooks/TestMutationWebhook.cs
using KubeOps.Operator.Web.Webhooks.Admission.Mutation;
using WebhookOperator.Entities; // Assuming V1TestEntity is defined here

namespace WebhookOperator.Webhooks;

[MutationWebhook(typeof(V1TestEntity))]
public class TestMutationWebhook : MutationWebhook<V1TestEntity>
{
    // Only implement methods for operations you want to mutate
    public override MutationResult<V1TestEntity> Create(V1TestEntity entity, bool dryRun)
    {
        if (entity.Spec.Username == "overwrite")
        {
            entity.Spec.Username = "random overwritten";
            // Return the modified entity
            return Modified(entity);
        }
        // Indicate no changes were made
        return NoChanges();
    }
}
```

## Conversion Webhooks

When you manage multiple versions of a Custom Resource Definition (CRD), Kubernetes needs a way to convert resources between these versions. This is essential for allowing users to interact with different API versions while maintaining a single storage version internally. KubeOps uses **Conversion Webhooks** for this.

See: [Kubernetes API Versioning](https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definition-versioning/#specify-multiple-versions)

### Implementation Steps

1.  **Add Package:** Ensure your operator project references `KubeOps.Operator.Web`.
2.  **Define Versions:** Define your different entity versions as separate C# classes (e.g., `V1Entity`, `V2Entity`, `V3Entity`), each marked with `[KubernetesEntity]`. Ensure your CRD definition in Kubernetes lists all supported versions and specifies one as the `storage` version.
3.  **Create Implementation:** Create a class inheriting from `ConversionWebhook<TStorageEntity>` (found in `KubeOps.Operator.Web.Webhooks.Conversion`), where `TStorageEntity` is the C# type corresponding to your designated `storage` version (e.g., `V3Entity`).
4.  **Add Attribute:** Decorate the implementation class with `[ConversionWebhook(typeof(TStorageEntity))]`.
5.  **Implement Converters:** Override the `Converters` property. This property must return a collection of objects, each implementing the bidirectional `IEntityConverter<TNonStorage, TStorage>` interface (found in `KubeOps.Operator.Web.Webhooks.Conversion`). Each implementation handles conversion between *one specific non-storage version* (`TNonStorage`) and the *storage version* (`TStorage`).
    *   Implement the `Convert(TNonStorage from)` method to convert from the non-storage version to the storage version.
    *   Implement the `Revert(TStorage storage)` method to convert from the storage version back to the non-storage version.
6.  **Register:** In your `Program.cs` or other startup code, register the webhook implementation using the dependency injection container:
    ```csharp
    // Example registration
    builder.Services.AddWebhook<MyConversionWebhook>(); // Registered via DI like other webhooks
    ```
7.  **Configure Kubernetes:** Update your CRD manifest:
    *   Define all supported versions under `spec.versions`.
    *   Set `spec.conversion.strategy` to `Webhook`.
    *   Configure `spec.conversion.webhook.clientConfig` to point to your operator's conversion webhook service endpoint (usually `/convert/{webhook-name}`).
    *   Specify the `spec.conversion.webhook.conversionReviewVersions` your webhook supports (e.g., `["v1", "v1beta1"]`).
    *   *Note:* The KubeOps [CLI Tool](./cli.md) can help generate CRDs with conversion settings (`dotnet kubeops generate crds ...`). This is the **recommended approach** to ensure the `conversion` stanza is correctly configured.

### Conversion Webhook Example

This example handles conversion between `V1TestEntity`, `V2TestEntity`, and `V3TestEntity`, where `V3TestEntity` is the storage version.

```csharp
// From examples/ConversionWebhookOperator/Webhooks/TestConversionWebhook.cs
using ConversionWebhookOperator.Entities; // v1, v2, v3 entities defined here
using KubeOps.Operator.Web.Webhooks.Conversion;

namespace ConversionWebhookOperator.Webhooks;

// Attribute specifies the STORAGE version
[ConversionWebhook(typeof(V3TestEntity))]
public class TestConversionWebhook : ConversionWebhook<V3TestEntity>
{
    // Provide converters for V1<->V3 and V2<->V3
    protected override IEnumerable<IEntityConverter<V3TestEntity>> Converters => new IEntityConverter<V3TestEntity>[]
    {
        new V1ToV3(), new V2ToV3(),
    };

    // Handles V1 <-> V3 conversion
    private class V1ToV3 : IEntityConverter<V1TestEntity, V3TestEntity>
    {
        public V3TestEntity Convert(V1TestEntity from)
        {
            // Logic to convert V1 spec to V3 spec
            var nameSplit = from.Spec.Name.Split(' ');
            var result = new V3TestEntity { Metadata = from.Metadata };
            result.Spec.Firstname = nameSplit[0];
            result.Spec.Lastname = string.Join(' ', nameSplit[1..]);
            return result;
        }

        public V1TestEntity Revert(V3TestEntity to)
        {
            // Logic to convert V3 spec back to V1 spec
            var result = new V1TestEntity { Metadata = to.Metadata };
            result.Spec.Name = $"{to.Spec.Firstname} {to.Spec.Lastname}";
            return result;
        }
    }

    // Handles V2 <-> V3 conversion
    private class V2ToV3 : IEntityConverter<V2TestEntity, V3TestEntity>
    {
        public V3TestEntity Convert(V2TestEntity from)
        {
            // Logic to convert V2 spec to V3 spec
            var result = new V3TestEntity { Metadata = from.Metadata };
            result.Spec.Firstname = from.Spec.Firstname;
            result.Spec.Lastname = from.Spec.Lastname;
            return result;
        }

        public V2TestEntity Revert(V3TestEntity to)
        {
            // Logic to convert V3 spec back to V2 spec
            var result = new V2TestEntity { Metadata = to.Metadata };
            result.Spec.Firstname = to.Spec.Firstname;
            result.Spec.Lastname = to.Spec.Lastname;
            return result;
        }
    }
}
```

Find the full conversion webhook example code in the GitHub repository: [https://github.com/ewassef/dotnet-operator-sdk/tree/main/examples/ConversionWebhookOperator/](https://github.com/ewassef/dotnet-operator-sdk/tree/main/examples/ConversionWebhookOperator/)

## Important Considerations

*   **TLS & Connectivity:** 
    *   Webhook endpoints *must* be served over HTTPS. The Kubernetes API server must be able to reach your operator's webhook service endpoint (usually via a Kubernetes `Service` of type `ClusterIP`).
    *   The API server must trust the TLS certificate presented by your operator's webhook endpoint. 
    *   **Production:** Tools like [cert-manager](https://cert-manager.io/) are commonly used to automate certificate provisioning and rotation within the cluster.
    *   **Development:** For local development (e.g., using `dotnet run` and port-forwarding or tools like [ngrok](https://ngrok.com/)), you might use self-signed certificates or disable TLS verification temporarily (not recommended for production!). KubeOps development templates often include helper scripts or configurations for local TLS setup.
    *   The generated `ValidatingWebhookConfiguration` or `MutatingWebhookConfiguration` includes a `clientConfig.caBundle` field where the CA certificate used to sign the webhook server's certificate must be placed, allowing the API server to verify the connection.
*   **RBAC:** Your operator's ServiceAccount needs appropriate RBAC permissions to get/list/watch the resources targeted by the webhooks, as well as permissions to manage `ValidatingWebhookConfiguration`, `MutatingWebhookConfiguration`, and potentially `CustomResourceDefinition` resources.
*   **Availability:** If your webhook service is down, the API server operations depending on it (creation, updates, reads of different versions) will fail. Ensure your operator deployment is highly available.
*   **Idempotency:** Mutation webhooks might be called multiple times for the same event. Ensure your mutation logic is idempotent (applying it multiple times has the same effect as applying it once).
