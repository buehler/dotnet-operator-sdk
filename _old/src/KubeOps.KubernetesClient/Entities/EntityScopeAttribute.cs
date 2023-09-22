namespace KubeOps.KubernetesClient.Entities;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EntityScopeAttribute : Attribute
{
    public EntityScopeAttribute(EntityScope scope = default)
    {
        Scope = scope;
    }

    public EntityScope Scope { get; }
}
