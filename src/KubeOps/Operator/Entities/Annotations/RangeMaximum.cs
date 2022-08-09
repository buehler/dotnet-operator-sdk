namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Defines a range maximum for a numeric property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RangeMaximumAttribute : Attribute
{
    /// <summary>
    /// Maximum value to be set.
    /// </summary>
    public double Maximum { get; init; }

    /// <summary>
    /// Defines if the maximum value is included or excluded.
    /// </summary>
    public bool ExclusiveMaximum { get; init; }
}
