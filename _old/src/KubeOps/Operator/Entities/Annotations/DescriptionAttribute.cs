namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Defines a description for a property. This precedes the description found in a
/// XML documentation file.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public class DescriptionAttribute : Attribute
{
    public DescriptionAttribute(string description)
    {
        Description = description;
    }

    /// <summary>
    /// The given description for the property.
    /// </summary>
    public string Description { get; }
}
