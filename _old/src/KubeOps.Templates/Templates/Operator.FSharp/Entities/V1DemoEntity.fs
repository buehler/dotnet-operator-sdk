namespace GeneratedOperatorProject.Entities

open k8s.Models
open KubeOps.Operator.Entities

type V1DemoEntitySpec() =
    member val Username = "" with get, set

type V1DemoEntityStatus() =
    member val DemoStatus = "" with get, set

[<KubernetesEntity(Group = "demo.kubeops.dev", ApiVersion = "v1", Kind = "DemoEntity")>]
type V1DemoEntity() =
    inherit CustomKubernetesEntity<V1DemoEntitySpec, V1DemoEntityStatus>()
