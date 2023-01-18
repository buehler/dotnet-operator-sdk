using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;

namespace KubeOps.Test.TestEntities;

public class TestStatusEntitySpec
{
    public string SpecString { get; set; } = string.Empty;
}

public class TestStatusEntityStatus
{
    public string StatusString { get; set; } = string.Empty;
    public List<ComplexStatusObject> StatusList { get; set; } = new List<ComplexStatusObject>();
}

public class ComplexStatusObject
{
    public string ObjectName { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}

[KubernetesEntity(Group = "kubeops.test.dev", ApiVersion = "V1")]
[KubernetesEntityShortNames("foo", "bar", "baz")]
public class TestStatusEntity : CustomKubernetesEntity<TestStatusEntitySpec, TestStatusEntityStatus>
{
}
