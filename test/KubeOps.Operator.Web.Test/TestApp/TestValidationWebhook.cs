using KubeOps.Operator.Web.Webhooks.Validation;

namespace KubeOps.Operator.Web.Test.TestApp;

[ValidationWebhook(typeof(V1OperatorWebIntegrationTestEntity))]
public class TestValidationWebhook : ValidationWebhook<V1OperatorWebIntegrationTestEntity>
{
    public override ValidationResult Create(V1OperatorWebIntegrationTestEntity entity, bool dryRun)
    {
        if (entity.Spec.Username == "forbidden")
        {
            return Fail("name may not be 'forbidden'.", 422);
        }

        return Success();
    }
}
