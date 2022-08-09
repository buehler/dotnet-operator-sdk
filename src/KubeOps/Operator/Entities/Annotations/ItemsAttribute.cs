namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Define minimum and maximum items count for an array property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ItemsAttribute : Attribute
{
    /// <summary>
    /// If not `-1`, defines the minimal item count for the property.
    /// -1 is used as the "null" - value.
    /// </summary>
    public long MinItems { get; init; } = -1;

    /// <summary>
    /// If not `-1`, defines the maximal item count for the property.
    /// -1 is used as the "null" - value.
    /// </summary>
    public long MaxItems { get; init; } = -1;
}
