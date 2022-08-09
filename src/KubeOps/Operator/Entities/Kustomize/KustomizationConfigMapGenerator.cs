namespace KubeOps.Operator.Entities.Kustomize;

/// <summary>
/// Entitiy for config map generators in a kustomization.yaml file.
/// </summary>
public class KustomizationConfigMapGenerator
{
    /// <summary>
    /// The name of the config map.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of files that should be added to the generated config map.
    /// </summary>
    public IList<string>? Files { get; set; }

    /// <summary>
    /// Config literals to add to the config map in the form of:
    /// - NAME=value.
    /// </summary>
    public IList<string>? Literals { get; set; }
}
