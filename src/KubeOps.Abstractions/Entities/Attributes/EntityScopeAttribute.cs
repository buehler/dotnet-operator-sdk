namespace KubeOps.Abstractions.Entities.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class EntityScopeAttribute(EntityScope scope = default) : Attribute
{
    public EntityScope Scope { get; } = scope;
}
