using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.Test.TestEntities;

[KubernetesEntity(
    ApiVersion = "v2",
    Kind = "AttributeVersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "attributeversionedentities")]
public class V2AttributeVersionedEntity : CustomKubernetesEntity
{
}
