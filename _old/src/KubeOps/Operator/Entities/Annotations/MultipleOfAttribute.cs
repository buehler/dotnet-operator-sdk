namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Defines the factor that a numeric value must adhere to.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MultipleOfAttribute : Attribute
{
    public MultipleOfAttribute(double value)
    {
        Value = value;
    }

    /// <summary>
    /// The property should be a multiple of this value.
    /// </summary>
    public double Value { get; }
}
