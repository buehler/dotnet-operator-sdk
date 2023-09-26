namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Defines length limits for properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class LengthAttribute : Attribute
{
    public LengthAttribute(long minLength = -1, long maxLength = -1)
    {
        MinLength = minLength switch
        {
            -1 => null,
            _ => minLength,
        };
        MaxLength = maxLength switch
        {
            -1 => null,
            _ => maxLength,
        };
    }

    /// <summary>
    /// Define the minimum length.
    /// </summary>
    public long? MinLength { get; }

    /// <summary>
    /// Define the maximum length.
    /// </summary>
    public long? MaxLength { get; }
}
