namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Defines a range maximum for a numeric property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RangeMaximumAttribute(double maximum, bool exclusiveMaximum = false) : Attribute
{
    /// <summary>
    /// Maximum value to be set.
    /// </summary>
    public double Maximum => maximum;

    /// <summary>
    /// Defines if the maximum value is included or excluded.
    /// </summary>
    public bool ExclusiveMaximum => exclusiveMaximum;
}
