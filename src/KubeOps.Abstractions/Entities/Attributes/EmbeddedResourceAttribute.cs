using k8s.Models;

namespace KubeOps.Abstractions.Entities.Attributes;

/// <summary>
/// Defines a property as an embedded resource.
/// This property can contain another kubernetes object
/// (e.g. a <see cref="V1ConfigMap"/> or a <see cref="V1Deployment"/>).
/// This implicitly sets the <see cref="PreserveUnknownFieldsAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class EmbeddedResourceAttribute : Attribute
{
}
