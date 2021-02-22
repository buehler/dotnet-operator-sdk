namespace GeneratedOperatorProject.Controller

open System
open System.Threading.Tasks
open GeneratedOperatorProject.Entities
open GeneratedOperatorProject.Finalizer
open k8s.Models
open KubeOps.Operator.Controller
open KubeOps.Operator.Controller.Results
open KubeOps.Operator.Finalizer
open KubeOps.Operator.Rbac
open Microsoft.Extensions.Logging

[<EntityRbac(typeof<V1DemoEntity>, Verbs = RbacVerb.All)>]
type DemoController(logger: ILogger<DemoController>, finalizerManager: IFinalizerManager<V1DemoEntity>) =
    interface IResourceController<V1DemoEntity> with
        member this.CreatedAsync entity =
            async {
                logger.LogInformation $"entity {entity.Name()} called created"

                do! finalizerManager.RegisterFinalizerAsync<DemoFinalizer> entity
                    |> Async.AwaitTask

                return
                    ResourceControllerResult.RequeueEvent
                    <| TimeSpan.FromSeconds 5.0
            }
            |> Async.StartAsTask

        member this.UpdatedAsync entity =
            logger.LogInformation $"entity {entity.Name()} called update"

            Task.FromResult
                (ResourceControllerResult.RequeueEvent
                 <| TimeSpan.FromSeconds 5.0)

        member this.NotModifiedAsync entity =
            logger.LogInformation $"entity {entity.Name()} called not modified"

            Task.FromResult
                (ResourceControllerResult.RequeueEvent
                 <| TimeSpan.FromSeconds 5.0)

        member this.StatusModifiedAsync entity =
            logger.LogInformation $"entity {entity.Name()} called not modified"
            Task.CompletedTask

        member this.DeletedAsync entity =
            logger.LogInformation $"entity {entity.Name()} called deleted"
            Task.CompletedTask
