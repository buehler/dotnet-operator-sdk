namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Defines the factor that a numeric value must adhere to.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MultipleOfAttribute(double value) : Attribute
{
    /// <summary>
    /// The property should be a multiple of this value.
    /// </summary>
    public double Value => value;
}
