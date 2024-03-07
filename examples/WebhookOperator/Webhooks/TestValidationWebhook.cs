using KubeOps.Operator.Web.Webhooks.Admission.Validation;

using WebhookOperator.Entities;

namespace WebhookOperator.Webhooks;

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
