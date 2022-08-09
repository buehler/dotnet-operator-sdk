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
        member this.ReconcileAsync entity =
            async {
                logger.LogInformation $"entity {entity.Name()} called reconcile"

                do! finalizerManager.RegisterFinalizerAsync<DemoFinalizer> entity
                    |> Async.AwaitTask

                return
                    ResourceControllerResult.RequeueEvent
                    <| TimeSpan.FromSeconds 15.0
            }
            |> Async.StartAsTask

        member this.StatusModifiedAsync entity =
            logger.LogInformation $"entity {entity.Name()} called not modified"
            Task.CompletedTask

        member this.DeletedAsync entity =
            logger.LogInformation $"entity {entity.Name()} called deleted"
            Task.CompletedTask
