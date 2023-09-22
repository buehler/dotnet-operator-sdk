namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// This attribute marks an entity as the storage version of
/// an entity. Only one storage version must be set.
/// If none of the versions define this attribute, the "newest"
/// one is taken according to the kubernetes versioning rules.
/// GA > Beta > Alpha > non versions.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class StorageVersionAttribute : Attribute
{
}
