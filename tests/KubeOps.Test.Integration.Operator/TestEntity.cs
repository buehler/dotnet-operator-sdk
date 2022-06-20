using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.Test.Integration.Operator;

public class V1TestEntitySpec
{
    /// <summary>
    /// This is a test for the contextual fetching of descriptions.
    /// </summary>
    public string Spec { get; set; } = string.Empty;
    public string PlaceHolder { get; set; } = string.Empty;

}

public class V1TestEntityStatus
{
    public string Status { get; set; } = string.Empty;
    public int ReconcileCounter { get; set; }

}

[KubernetesEntity(Group = "integration.testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
public class V1TestEntity : CustomKubernetesEntity<V1TestEntitySpec, V1TestEntityStatus>
{
}

public class V2TestEntitySpec
{
    /// <summary>
    /// This is a test for the contextual fetching of descriptions.
    /// </summary>
    public string Spec { get; set; } = string.Empty;
    public string PlaceHolder { get; set; } = string.Empty;

}

public class V2TestEntityStatus
{
    public string Status { get; set; } = string.Empty;
    public int ReconcileCounter { get; set; }

}

[KubernetesEntity(Group = "integration.testing.dev", ApiVersion = "v2", Kind = "TestEntity")]
public class V2TestEntity : CustomKubernetesEntity<V2TestEntitySpec, V2TestEntityStatus>
{
}
