# Webhooks

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
5.  **Register:** In your `Program.cs`, register the webhook implementation: `builder.Services.AddWebhook<MyValidationWebhook>();` or `builder.Services.AddWebhook<MyMutationWebhook>();`
6.  **Configure Kubernetes:** Create `ValidatingWebhookConfiguration` or `MutatingWebhookConfiguration` Kubernetes resources. These tell the API server to send admission requests for specific resources/operations to your operator's webhook service endpoint (usually `/mutate/{webhook-name}` or `/validate/{webhook-name}`).
    *   *Note:* The `KubeOps.Cli` tool can help generate these configurations based on your webhook attributes (`dotnet kubeops generate webhooks ...`).

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

Find the full example code here: [`examples/WebhookOperator/Webhooks`](../../../examples/WebhookOperator/Webhooks).

## Conversion Webhooks

When you manage multiple versions of a Custom Resource Definition (CRD), Kubernetes needs a way to convert resources between these versions. This is essential for allowing users to interact with different API versions while maintaining a single storage version internally. KubeOps uses **Conversion Webhooks** for this.

See: [Kubernetes API Versioning](https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definition-versioning/#specify-multiple-versions)

### Implementation Steps

1.  **Add Package:** Ensure your operator project references `KubeOps.Operator.Web`.
2.  **Define Versions:** Define your different entity versions as separate C# classes (e.g., `V1Entity`, `V2Entity`, `V3Entity`), each marked with `[KubernetesEntity]`. Ensure your CRD definition in Kubernetes lists all supported versions and specifies one as the `storage` version.
3.  **Create Implementation:** Create a class inheriting from `ConversionWebhook<TStorageEntity>` (found in `KubeOps.Operator.Web.Webhooks.Conversion`), where `TStorageEntity` is the C# type corresponding to your designated `storage` version (e.g., `V3Entity`).
4.  **Add Attribute:** Decorate the implementation class with `[ConversionWebhook(typeof(TStorageEntity))]`.
5.  **Implement Converters:** Override the `Converters` property. This property must return a collection of objects, each implementing `IEntityConverter<TFrom, TStorage>` or `IEntityConverter<TStorage, TTo>`. Each converter handles bidirectional conversion between *one specific non-storage version* and the *storage version*.
    *   Implement the `Convert(TFrom from)` method to convert from the non-storage version to the storage version.
    *   Implement the `Revert(TStorage to)` method to convert from the storage version back to the non-storage version.
6.  **Register:** In your `Program.cs`, register the webhook implementation: `builder.Services.AddWebhook<MyConversionWebhook>();`
7.  **Configure Kubernetes:** Update your CRD manifest:
    *   Define all supported versions under `spec.versions`.
    *   Set `spec.conversion.strategy` to `Webhook`.
    *   Configure `spec.conversion.webhook.clientConfig` to point to your operator's conversion webhook service endpoint (usually `/convert/{webhook-name}`).
    *   Specify the `spec.conversion.webhook.conversionReviewVersions` your webhook supports (e.g., `["v1", "v1beta1"]`).
    *   *Note:* The `KubeOps.Cli` tool can help generate CRDs with conversion settings (`dotnet kubeops generate crds ...`).

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

Find the full example code here: [`examples/ConversionWebhookOperator`](../../../examples/ConversionWebhookOperator).

## Important Considerations

*   **TLS:** Webhook endpoints *must* be served over HTTPS. Kubernetes needs to trust the certificate served by your operator. Managing certificates and configuring the API server can be complex. Tools like [cert-manager](https://cert-manager.io/) are often used.
*   **RBAC:** Your operator's ServiceAccount needs appropriate RBAC permissions to get/list/watch the resources targeted by the webhooks, as well as permissions to manage `ValidatingWebhookConfiguration`, `MutatingWebhookConfiguration`, and potentially `CustomResourceDefinition` resources.
*   **Availability:** If your webhook service is down, the API server operations depending on it (creation, updates, reads of different versions) will fail. Ensure your operator deployment is highly available.
*   **Idempotency:** Mutation webhooks might be called multiple times for the same event. Ensure your mutation logic is idempotent (applying it multiple times has the same effect as applying it once).
