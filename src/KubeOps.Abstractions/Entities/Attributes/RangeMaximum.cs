namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Defines a range maximum for a numeric property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RangeMaximumAttribute : Attribute
{
    public RangeMaximumAttribute(double maximum, bool exclusiveMaximum = false)
    {
        Maximum = maximum;
        ExclusiveMaximum = exclusiveMaximum;
    }

    /// <summary>
    /// Maximum value to be set.
    /// </summary>
    public double Maximum { get; }

    /// <summary>
    /// Defines if the maximum value is included or excluded.
    /// </summary>
    public bool ExclusiveMaximum { get; }
}
