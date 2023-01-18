namespace KubeOps.KubernetesClient.Entities;

/// <summary>
/// Entity ("resource") definition. This is not a full CRD (custom resource definition) of
/// Kubernetes, but all parts that regard the resource. This is used to construct a CRD out of a type
/// of kubernetes entities/resources.
/// </summary>
public readonly struct EntityDefinition
{
    public readonly string Kind;

    public readonly string ListKind;

    public readonly string Group;

    public readonly string Version;

    public readonly string Singular;

    public readonly string Plural;

    public readonly EntityScope Scope;

    public EntityDefinition(
        string kind,
        string listKind,
        string group,
        string version,
        string singular,
        string plural,
        EntityScope scope)
    {
        Kind = kind;
        ListKind = listKind;
        Group = group;
        Version = version;
        Singular = singular;
        Plural = plural;
        Scope = scope;
    }

    public static EntityDefinition FromType<T>() => typeof(T).ToEntityDefinition();
}
