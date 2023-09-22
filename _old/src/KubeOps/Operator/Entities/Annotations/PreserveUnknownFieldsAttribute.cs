namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Defines that a property should keep unknown fields
/// so that kubernetes does not purge additional structures.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PreserveUnknownFieldsAttribute : Attribute
{
}
