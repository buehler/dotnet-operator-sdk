using k8s.Models;

using KubeOps.Abstractions.Entities;

namespace Operator.Entities;

[KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "SecondEntity")]
public partial class V1SecondEntity : CustomKubernetesEntity
{
    public override string ToString() => $"Second Entity ({Metadata.Name})";
}
