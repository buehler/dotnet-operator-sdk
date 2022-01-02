namespace KubeOps.Operator.Entities.Kustomize;

/// <summary>
/// Definition for an "image" in a kustomization yaml.
/// </summary>
public class KustomizationImage
{
    /// <summary>
    /// Name of the image.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// New name of the image.
    /// </summary>
    public string NewName { get; set; } = string.Empty;

    /// <summary>
    /// New tag of the image.
    /// </summary>
    public string NewTag { get; set; } = string.Empty;
}
