namespace KubeOps.Abstractions.Rbac;

/// <summary>
/// Abstract base class for all RBAC attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public abstract class RbacAttribute : Attribute;
