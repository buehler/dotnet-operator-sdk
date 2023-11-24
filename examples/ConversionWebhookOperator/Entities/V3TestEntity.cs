using k8s.Models;

using KubeOps.Abstractions.Entities;

namespace ConversionWebhookOperator.Entities;

[KubernetesEntity(Group = "conversion-webhook.dev", ApiVersion = "v3", Kind = "TestEntity")]
public partial class V3TestEntity : CustomKubernetesEntity<V3TestEntity.EntitySpec>
{
    public override string ToString() => $"Test Entity v3 ({Metadata.Name}): {Spec.Firstname} {Spec.MiddleName} {Spec.Lastname}";

    public class EntitySpec
    {
        public string Firstname { get; set; } = string.Empty;

        public string Lastname { get; set; } = string.Empty;

        public string? MiddleName { get; set; }
    }
}
