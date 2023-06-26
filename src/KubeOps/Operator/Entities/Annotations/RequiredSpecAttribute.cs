namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Allows to mark Entity specification as required.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RequiredSpecAttribute : Attribute
{
}
