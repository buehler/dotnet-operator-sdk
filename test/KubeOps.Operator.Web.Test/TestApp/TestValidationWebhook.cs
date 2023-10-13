using KubeOps.Operator.Web.Webhooks.Validation;

namespace KubeOps.Operator.Web.Test.TestApp;

[ValidationWebhook(typeof(V1IntegrationTestEntity))]
public class TestValidationWebhook : ValidationWebhook<V1IntegrationTestEntity>
{
    public override ValidationResult Create(V1IntegrationTestEntity entity, bool dryRun)
    {
        if (entity.Spec.Username == "forbidden")
        {
            return Fail("name may not be 'forbidden'.", 422);
        }

        return Success();
    }

    public override ValidationResult Update(
        V1IntegrationTestEntity oldEntity,
        V1IntegrationTestEntity newEntity,
        bool dryRun)
    {
        if (newEntity.Spec.Username == "forbidden")
        {
            return Fail("name may not be 'forbidden'.");
        }

        return Success();
    }
}
