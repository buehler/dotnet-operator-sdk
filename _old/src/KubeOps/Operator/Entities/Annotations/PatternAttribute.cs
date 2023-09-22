namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Define a regex validator for the property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PatternAttribute : Attribute
{
    public PatternAttribute(string regexPattern)
    {
        RegexPattern = regexPattern;
    }

    /// <summary>
    /// The regex pattern to be used.
    /// </summary>
    public string RegexPattern { get; }
}
