using k8s.Models;

using KubeOps.Abstractions.Entities;

namespace WebhookOperator.Entities;

[KubernetesEntity(Group = "webhook.dev", ApiVersion = "v1", Kind = "TestEntity")]
public partial class V1TestEntity : CustomKubernetesEntity<V1TestEntity.EntitySpec>
{
    public override string ToString() => $"Test Entity v1 ({Metadata.Name}): {Spec.Name}";

    public class EntitySpec
    {
        public string Name { get; set; } = string.Empty;
    }
}
