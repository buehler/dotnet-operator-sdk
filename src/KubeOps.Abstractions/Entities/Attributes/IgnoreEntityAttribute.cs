namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Attribute that states that the given entity or property should be
/// ignored during CRD generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class IgnoreAttribute : Attribute;
