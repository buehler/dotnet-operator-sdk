namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Defines a property of a specification as required.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RequiredAttribute : Attribute
{
}
