namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Defines that the property has an external documentation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ExternalDocsAttribute(string url, string? description = null) : Attribute
{
    /// <summary>
    /// Additional description.
    /// </summary>
    public string? Description => description;

    /// <summary>
    /// Url where to find the documentation.
    /// </summary>
    public string Url => url;
}
