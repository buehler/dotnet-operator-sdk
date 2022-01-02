using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;

namespace KubeOps.Test.TestEntities;

[IgnoreEntity]
[KubernetesEntity(Group = "kubeops.test.dev", ApiVersion = "V1")]
public class TestIgnoredEntity : CustomKubernetesEntity
{
}
