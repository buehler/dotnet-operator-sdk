using k8s;

namespace KubeOps.Operator.Entities.Kustomize;

/// <summary>
/// (Partial) definition for a kustomization yaml.
/// </summary>
public class KustomizationConfig : KubernetesObject
{
    public KustomizationConfig()
    {
        ApiVersion = "kustomize.config.k8s.io/v1beta1";
        Kind = "Kustomization";
    }

    /// <summary>
    /// Namespace that should be set.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Name prefix that should be set.
    /// </summary>
    public string? NamePrefix { get; set; }

    /// <summary>
    /// Common labels for the resources.
    /// </summary>
    public IDictionary<string, string>? CommonLabels { get; set; }

    /// <summary>
    /// Resource list.
    /// </summary>
    public IList<string>? Resources { get; set; }

    /// <summary>
    /// List of merge patches.
    /// </summary>
    public IList<string>? PatchesStrategicMerge { get; set; }

    /// <summary>
    /// List of <see cref="KustomizationImage"/>.
    /// </summary>
    public IList<KustomizationImage>? Images { get; set; }

    /// <summary>
    /// List of <see cref="KustomizationConfigMapGenerator"/>.
    /// </summary>
    public IList<KustomizationConfigMapGenerator>? ConfigMapGenerator { get; set; }
}
