namespace KubeOps.Abstractions.Entities.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class EntityScopeAttribute : Attribute
{
    public EntityScopeAttribute(EntityScope scope = default)
    {
        Scope = scope;
    }

    public EntityScope Scope { get; }
}
