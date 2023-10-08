using k8s;
using k8s.Models;

using KubeOps.Abstractions.Entities.Attributes;

namespace KubeOps.Cli.Test.TestEntities;

public class TestStatusEntitySpec
{
    public string SpecString { get; set; } = string.Empty;
}

public class TestStatusEntityStatus
{
    public string StatusString { get; set; } = string.Empty;
    public List<ComplexStatusObject> StatusList { get; set; } = new();
}

public class ComplexStatusObject
{
    public string ObjectName { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}

[KubernetesEntity(Group = "kubeops.test.dev", ApiVersion = "V1")]
[KubernetesEntityShortNames("foo", "bar", "baz")]
public class TestStatusEntity : IKubernetesObject<V1ObjectMeta>, ISpec<TestStatusEntitySpec>,
    IStatus<TestStatusEntityStatus>
{
    public string ApiVersion { get; set; } = "kubeops.test.dev/v1";
    public string Kind { get; set; } = "TestStatusEntity";
    public V1ObjectMeta Metadata { get; set; } = new();
    public TestStatusEntitySpec Spec { get; set; } = new();
    public TestStatusEntityStatus Status { get; set; } = new();
}
