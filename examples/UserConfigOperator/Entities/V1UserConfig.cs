using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace UserConfigOperator.Entities;

[KubernetesEntity(Kind = "UserConfig", ApiVersion = "v1", Group = "demo-operators.io")]
public partial class V1UserConfig : CustomKubernetesEntity<V1UserConfig.EntitySpec>
{
    public class EntitySpec
    {
        [Required]
        public string Username { get; set; } = string.Empty;
    }
}
