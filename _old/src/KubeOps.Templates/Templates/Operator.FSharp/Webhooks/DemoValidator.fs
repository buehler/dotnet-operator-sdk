namespace GeneratedOperatorProject.Webhooks

open GeneratedOperatorProject.Entities
open KubeOps.Operator.Webhooks
open Microsoft.AspNetCore.Http

type DemoValidator() =
    interface IValidationWebhook<V1DemoEntity> with
        member this.Operations = AdmissionOperations.Create

        member this.Create(newEntity, _) =
            if newEntity.Spec.Username = "forbiddenUsername"
            then ValidationResult.Fail(StatusCodes.Status400BadRequest, "Username is forbidden")
            else ValidationResult.Success()
