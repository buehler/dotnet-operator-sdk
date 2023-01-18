using k8s.Models;
using KubeOps.KubernetesClient.Entities;
using KubeOps.Operator.Entities;

namespace KubeOps.TestOperator.Entities;

public class V1ClusterTestEntitySpec
{
    /// <summary>
    /// This is a test for the contextual fetching of descriptions.
    /// </summary>
    public string Spec { get; set; } = string.Empty;
}

public class V1ClusterTestEntityStatus
{
    public string Status { get; set; } = string.Empty;
}

[KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "ClusterTestEntity")]
[EntityScope(EntityScope.Cluster)]
public class V1ClusterTestEntity : CustomKubernetesEntity<V1ClusterTestEntitySpec, V1ClusterTestEntityStatus>
{
}
