using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;

namespace KubeOps.Test.TestEntities;

[IgnoreEntity]
[KubernetesEntity(Group = "kubeops.test.dev", ApiVersion = "V1")]
public class TestInvalidEntity : CustomKubernetesEntity
{
    public ulong Unsupported { get; set; }

    public ulong? NullableUnsupported { get; set; }
}
