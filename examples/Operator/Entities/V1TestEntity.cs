using k8s;
using k8s.Models;

namespace Operator.Entities;

[KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
public class V1TestEntity : IKubernetesObject<V1ObjectMeta>, ISpec<V1TestEntitySpec>, IStatus<V1TestEntityStatus>
{
    public string ApiVersion { get; set; } = "testing.dev/v1";

    public string Kind { get; set; } = "TestEntity";

    public V1ObjectMeta Metadata { get; set; } = new();

    public V1TestEntitySpec Spec { get; set; } = new();

    public V1TestEntityStatus Status { get; set; } = new();
}
