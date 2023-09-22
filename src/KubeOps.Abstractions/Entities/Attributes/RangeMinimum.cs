namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Defines a range minimum for a numeric property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RangeMinimumAttribute : Attribute
{
    public RangeMinimumAttribute(double minimum, bool exclusiveMinimum = false)
    {
        Minimum = minimum;
        ExclusiveMinimum = exclusiveMinimum;
    }

    /// <summary>
    /// Minimum value to be set.
    /// </summary>
    public double Minimum { get; }

    /// <summary>
    /// Defines if the minimum value is included or excluded.
    /// </summary>
    public bool ExclusiveMinimum { get; }
}
