using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.Test.Operator.Entities.TestEntities
{
    public class TestStatusEntitySpec
    {
    }

    public class TestStatusEntityStatus
    {
    }

    [KubernetesEntity(Group = "kubeops.test.dev", ApiVersion = "V1")]
    public class TestStatusEntity : CustomKubernetesEntity<TestStatusEntitySpec, TestStatusEntityStatus>
    {
    }
}
