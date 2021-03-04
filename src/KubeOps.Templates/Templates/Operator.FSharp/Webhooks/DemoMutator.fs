namespace GeneratedOperatorProject.Webhooks

open GeneratedOperatorProject.Entities
open KubeOps.Operator.Webhooks
open Microsoft.AspNetCore.Http

type DemoMutator() =
    interface IMutationWebhook<V1DemoEntity> with
        member this.Operations = AdmissionOperations.Create

        member this.Create(newEntity, _) =
            newEntity.Spec.Username <- "not foobar"
            MutationResult.Modified(newEntity)
