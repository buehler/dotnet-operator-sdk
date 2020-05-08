using System.Collections.Generic;
using k8s;

namespace KubeOps.Operator.Entities.Kustomize
{
    public class KustomizationConfig : KubernetesObject
    {
        public KustomizationConfig()
        {
            ApiVersion = "kustomize.config.k8s.io/v1beta1";
            Kind = "Kustomization";
        }

        public string? Namespace { get; set; }

        public string? NamePrefix { get; set; }

        public IDictionary<string, string>? CommonLabels { get; set; }

        public IList<string>? Resources { get; set; }

        public IList<string>? PatchesStrategicMerge { get; set; }

        public IList<KustomizationImage>? Images { get; set; }
    }
}
