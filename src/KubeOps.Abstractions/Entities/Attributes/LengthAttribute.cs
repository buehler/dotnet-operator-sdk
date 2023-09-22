namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Defines length limits for properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class LengthAttribute : Attribute
{
    /// <summary>
    /// Define the minimum length.
    /// </summary>
#if NETSTANDARD
    public long? MinLength { get; set; }
#else
    public long? MinLength { get; init; }
#endif

    /// <summary>
    /// Define the maximum length.
    /// </summary>
#if NETSTANDARD
    public long? MaxLength { get; set; }
#else
    public long? MaxLength { get; init; }
#endif
}
