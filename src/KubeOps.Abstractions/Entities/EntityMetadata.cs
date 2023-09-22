namespace KubeOps.Abstractions.Entities;

#if NET
public record EntityMetadata(string Kind, string Version, string? Group = null, string? Plural = null)
{
    public string ListKind => $"{Kind}List";

    public string SingularName => Kind.ToLower();

    public string PluralName => Plural ?? $"{Kind.ToLower()}s";
}
#else
public class EntityMetadata
{
    public EntityMetadata(string kind, string version, string? group = null, string? plural = null)
    {
        Kind = kind;
        Version = version;
        Group = group;
        Plural = plural;
    }

    public string Kind { get; }

    public string Version { get; }

    public string? Group { get; }

    public string? Plural { get; }

    public string ListKind => $"{Kind}List";

    public string SingularName => Kind.ToLower();

    public string PluralName => Plural ?? $"{Kind.ToLower()}s";
}
#endif
