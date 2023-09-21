namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Attribute that states that the given property should be
/// ignored during CRD generation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IgnorePropertyAttribute : Attribute
{
}
