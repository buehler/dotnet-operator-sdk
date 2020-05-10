using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.TestOperator.Entities
{
    public class TestEntitySpec
    {
        public string Spec { get; set; } = string.Empty;
    }

    public class TestEntityStatus
    {
        public string Status { get; set; } = string.Empty;
    }

    [KubernetesEntity(Group = "testing", ApiVersion = "v1")]
    public class TestEntity : CustomKubernetesEntity<TestEntitySpec, TestEntityStatus>
    {
    }
}
