namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Define "shortNames" for CRDs.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class KubernetesEntityShortNamesAttribute(params string[] shortNames) : Attribute
{
    /// <summary>
    /// Array of shortnames that should be attached to CRDs.
    /// </summary>
    public string[] ShortNames => shortNames;
}
