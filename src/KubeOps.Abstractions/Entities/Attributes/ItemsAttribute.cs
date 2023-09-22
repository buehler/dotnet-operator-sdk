namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Define minimum and maximum items count for an array property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ItemsAttribute : Attribute
{
    /// <summary>
    /// Defines the minimal item count for the property.
    /// </summary>
#if NETSTANDARD
    public long? MinItems { get; set; }
#else
    public long? MinItems { get; init; }
#endif

    /// <summary>
    /// Defines the maximal item count for the property.
    /// </summary>
#if NETSTANDARD
    public long? MaxItems { get; set; }
#else
    public long? MaxItems { get; init; }
#endif
}
