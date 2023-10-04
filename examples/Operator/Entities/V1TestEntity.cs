using k8s.Models;

using KubeOps.Abstractions.Entities;

namespace Operator.Entities;

[KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
public partial class V1TestEntity : CustomKubernetesEntity<V1TestEntity.EntitySpec, V1TestEntity.EntityStatus>
{
    public override string ToString() => $"Test Entity ({Metadata.Name}): {Spec.Username} ({Spec.Email})";

    public class EntitySpec
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }

    public class EntityStatus
    {
        public string Status { get; set; } = string.Empty;
    }
}
