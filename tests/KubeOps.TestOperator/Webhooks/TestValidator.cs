using KubeOps.Operator.Webhooks;
using KubeOps.TestOperator.Entities;
using Microsoft.AspNetCore.Http;

namespace KubeOps.TestOperator.Webhooks;

public class TestValidator : IValidationWebhook<V2TestEntity>
{
    public AdmissionOperations Operations => AdmissionOperations.Create | AdmissionOperations.Update;

    public ValidationResult Create(V2TestEntity newEntity, bool dryRun) =>
        CheckSpec(newEntity)
            ? ValidationResult.Success("The username may not be foobar.")
            : ValidationResult.Fail(StatusCodes.Status400BadRequest, @"Username is ""foobar"".");

    public ValidationResult Update(V2TestEntity _, V2TestEntity newEntity, bool dryRun) =>
        CheckSpec(newEntity)
            ? ValidationResult.Success("The username may not be foobar.")
            : ValidationResult.Fail(StatusCodes.Status400BadRequest, @"Username is ""foobar"".");

    private static bool CheckSpec(V2TestEntity entity) => entity.Spec.Username != "foobar";
}
