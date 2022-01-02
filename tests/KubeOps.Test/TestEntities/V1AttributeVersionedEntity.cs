using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;

namespace KubeOps.Test.TestEntities;

[KubernetesEntity(
    ApiVersion = "v1",
    Kind = "AttributeVersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "attributeversionedentities")]
[StorageVersion]
public class V1AttributeVersionedEntity : CustomKubernetesEntity
{
}
