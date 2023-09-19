namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Define "shortNames" for CRDs.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class KubernetesEntityShortNamesAttribute : Attribute
{
    public KubernetesEntityShortNamesAttribute(params string[] shortNames)
    {
        ShortNames = shortNames;
    }

    /// <summary>
    /// Array of shortnames that should be attached to CRDs.
    /// </summary>
    public string[] ShortNames { get; }
}
