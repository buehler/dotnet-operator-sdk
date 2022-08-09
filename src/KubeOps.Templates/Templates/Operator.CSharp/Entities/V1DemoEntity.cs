using k8s.Models;
using KubeOps.Operator.Entities;

namespace GeneratedOperatorProject.Entities;

[KubernetesEntity(Group = "demo.kubeops.dev", ApiVersion = "v1", Kind = "DemoEntity")]
public class V1DemoEntity : CustomKubernetesEntity<V1DemoEntity.V1DemoEntitySpec, V1DemoEntity.V1DemoEntityStatus>
{
    public class V1DemoEntitySpec
    {
        public string Username { get; set; } = string.Empty;
    }

    public class V1DemoEntityStatus
    {
        public string DemoStatus { get; set; } = string.Empty;
    }
}
