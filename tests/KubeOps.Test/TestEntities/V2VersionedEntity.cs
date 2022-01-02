using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.Test.TestEntities;

[KubernetesEntity(
    ApiVersion = "v2",
    Kind = "VersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "versionedentities")]
public class V2VersionedEntity : CustomKubernetesEntity
{
}
