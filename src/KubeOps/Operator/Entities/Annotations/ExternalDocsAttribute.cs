namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Defines that the property has an external documentation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ExternalDocsAttribute : Attribute
{
    public ExternalDocsAttribute(string url, string? description = null)
    {
        Description = description;
        Url = url;
    }

    /// <summary>
    /// Additional description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Url where to find the documentation.
    /// </summary>
    public string Url { get; }
}
