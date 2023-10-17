using k8s.Models;

using KubeOps.Abstractions.Entities;

namespace WebhookOperator.Entities;

[KubernetesEntity(Group = "webhook.dev", ApiVersion = "v2", Kind = "TestEntity")]
public partial class V2TestEntity : CustomKubernetesEntity<V2TestEntity.EntitySpec>
{
    public override string ToString() => $"Test Entity v2 ({Metadata.Name}): {Spec.Firstname} {Spec.Lastname}";

    public class EntitySpec
    {
        public string Firstname { get; set; } = string.Empty;

        public string Lastname { get; set; } = string.Empty;
    }
}
