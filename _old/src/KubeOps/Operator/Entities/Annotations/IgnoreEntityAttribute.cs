namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Attribute that states that the given entity should be
/// ignored during CRD generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class IgnoreEntityAttribute : Attribute
{
}
