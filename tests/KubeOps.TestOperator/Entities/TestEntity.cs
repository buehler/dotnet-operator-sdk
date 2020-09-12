using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.TestOperator.Entities
{
    public class TestEntitySpec
    {
        /// <summary>
        /// This is a test for the contextual fetching of descriptions.
        /// </summary>
        public string Spec { get; set; } = string.Empty;
    }

    public class TestEntityStatus
    {
        public string Status { get; set; } = string.Empty;
    }

    [KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", PluralName = "testentities")]
    public class TestEntity : CustomKubernetesEntity<TestEntitySpec, TestEntityStatus>
    {
    }
}
