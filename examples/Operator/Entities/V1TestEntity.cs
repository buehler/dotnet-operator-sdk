using k8s;
using k8s.Models;

namespace Operator.Entities;

[KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
public class V1TestEntity : IKubernetesObject<V1ObjectMeta>
{
    public string ApiVersion { get; set; }

    public string Kind { get; set; }

    public V1ObjectMeta Metadata { get; set; }

    public V1TestEntitySpec Spec { get; set; } = new();

    public V1TestEntityStatus Status { get; set; } = new();
}
