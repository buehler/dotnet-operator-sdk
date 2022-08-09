namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Defines a range minimum for a numeric property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RangeMinimumAttribute : Attribute
{
    /// <summary>
    /// Minimum value to be set.
    /// </summary>
    public double Minimum { get; init; }

    /// <summary>
    /// Defines if the minimum value is included or excluded.
    /// </summary>
    public bool ExclusiveMinimum { get; init; }
}
