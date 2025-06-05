using KubeOps.Operator.Web.Webhooks.Admission.Validation;

using GeneratedOperatorProject.Entities;

namespace GeneratedOperatorProject.Webhooks;

[ValidationWebhook(typeof(V1DemoEntity))]
public class TestValidationWebhook : ValidationWebhook<V1DemoEntity>
{
    public override ValidationResult Create(V1DemoEntity entity, bool dryRun)
    {
        if (entity.Spec.Username == "forbidden")
        {
            return Fail("name may not be 'forbidden'.", 422);
        }

        return Success();
    }

    public override ValidationResult Update(V1DemoEntity oldEntity, V1DemoEntity newEntity, bool dryRun)
    {
        if (newEntity.Spec.Username == "forbidden")
        {
            return Fail("name may not be 'forbidden'.");
        }

        return Success();
    }
}
