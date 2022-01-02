using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;

namespace KubeOps.TestOperator.Entities;

public class V2TestEntitySpec
{
    /// <summary>
    /// This is a test for the contextual fetching of descriptions.
    /// </summary>
    public string Spec { get; set; } = string.Empty;

    [AdditionalPrinterColumn]
    public string Username { get; set; } = string.Empty;

    public IntstrIntOrString StringOrInteger { get; set; } = 42;
}

public class V2TestEntityStatus
{
    public string Status { get; set; } = string.Empty;
}

[KubernetesEntity(Group = "testing.dev", ApiVersion = "v2", Kind = "TestEntity")]
[GenericAdditionalPrinterColumn(".metadata.namespace", "Namespace", "string")]
public class V2TestEntity : CustomKubernetesEntity<V2TestEntitySpec, V2TestEntityStatus>
{
}
