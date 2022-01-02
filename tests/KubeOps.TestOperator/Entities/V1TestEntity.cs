using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.TestOperator.Entities;

public class V1TestEntitySpec
{
    /// <summary>
    /// This is a test for the contextual fetching of descriptions.
    /// </summary>
    public string Spec { get; set; } = string.Empty;
}

public class V1TestEntityStatus
{
    public string Status { get; set; } = string.Empty;
}

[KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
public class V1TestEntity : CustomKubernetesEntity<V1TestEntitySpec, V1TestEntityStatus>
{
}
