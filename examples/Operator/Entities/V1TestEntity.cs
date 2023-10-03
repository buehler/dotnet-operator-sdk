using k8s.Models;

using KubeOps.Abstractions.Entities;

namespace Operator.Entities;

[KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
public class V1TestEntity : CustomKubernetesEntity<V1TestEntitySpec, V1TestEntityStatus>
{
    public override string ToString() => $"Test Entity ({Metadata.Name}): {Spec.Username} ({Spec.Email})";
}
