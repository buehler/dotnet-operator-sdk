namespace GeneratedOperatorProject.Finalizer

open System.Threading.Tasks
open GeneratedOperatorProject.Entities
open k8s.Models
open KubeOps.Operator.Finalizer
open Microsoft.Extensions.Logging

type DemoFinalizer(logger: ILogger<DemoFinalizer>) =
    interface IResourceFinalizer<V1DemoEntity> with
        member this.FinalizeAsync entity =
            logger.LogInformation $"entity {entity.Name()} called finalize"
            Task.CompletedTask
