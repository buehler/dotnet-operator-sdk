using k8s;

namespace KubeOps.Cli.Test.TestEntities;

public abstract class Base : IKubernetesObject
{
    public string ApiVersion { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
}
