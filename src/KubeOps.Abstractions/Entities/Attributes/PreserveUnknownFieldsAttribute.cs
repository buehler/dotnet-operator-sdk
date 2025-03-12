namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Defines that a property should keep unknown fields
/// so that kubernetes does not purge additional structures.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public class PreserveUnknownFieldsAttribute : Attribute;
