using GeneratedOperatorProject.Entities;
using KubeOps.Operator.Webhooks;
using Microsoft.AspNetCore.Http;

namespace GeneratedOperatorProject.Webhooks
{
    public class DemoValidator : IValidationWebhook<V1DemoEntity>
    {
        public ValidatedOperations Operations => ValidatedOperations.Create;

        public ValidationResult Create(V1DemoEntity newEntity, bool dryRun)
            => newEntity.Spec.Username == "forbiddenUsername"
                ? ValidationResult.Fail(StatusCodes.Status400BadRequest, "Username is forbidden")
                : ValidationResult.Success();
    }
}
