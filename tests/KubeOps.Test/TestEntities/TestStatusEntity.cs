using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.Test.TestEntities
{
    public class TestStatusEntitySpec
    {
        public string SpecString { get; set; } = string.Empty;
    }

    public class TestStatusEntityStatus
    {
        public string StatusString { get; set; } = string.Empty;
    }

    [KubernetesEntity(Group = "kubeops.test.dev", ApiVersion = "V1")]
    public class TestStatusEntity : CustomKubernetesEntity<TestStatusEntitySpec, TestStatusEntityStatus>
    {
    }
}
