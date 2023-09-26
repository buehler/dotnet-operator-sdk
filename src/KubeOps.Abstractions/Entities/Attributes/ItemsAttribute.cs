namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Define minimum and maximum items count for an array property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ItemsAttribute : Attribute
{
    public ItemsAttribute(long minItems = -1, long maxItems = -1)
    {
        MinItems = minItems switch
        {
            -1 => null,
            _ => minItems,
        };
        MaxItems = maxItems switch
        {
            -1 => null,
            _ => maxItems,
        };
    }

    /// <summary>
    /// Defines the minimal item count for the property.
    /// </summary>
    public long? MinItems { get; }

    /// <summary>
    /// Defines the maximal item count for the property.
    /// </summary>
    public long? MaxItems { get; }
}
