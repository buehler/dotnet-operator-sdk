namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Defines length limits for properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class LengthAttribute : Attribute
{
    /// <summary>
    /// If not `-1`, define the minimum length.
    /// -1 is used as the "null" - value.
    /// </summary>
    public long MinLength { get; init; } = -1;

    /// <summary>
    /// If not `-1`, define the maximum length.
    /// -1 is used as the "null" - value.
    /// </summary>
    public long MaxLength { get; init; } = -1;
}
