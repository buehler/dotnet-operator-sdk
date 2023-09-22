namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Attribute that states that the given entity should be
/// ignored during CRD generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class IgnoreEntityAttribute : Attribute
{
}
