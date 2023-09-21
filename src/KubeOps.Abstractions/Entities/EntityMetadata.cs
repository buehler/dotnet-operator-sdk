namespace KubeOps.Abstractions.Entities;

public record EntityMetadata(string Kind, string Version, string? Group = null, string? Plural = null)
{
    public string ListKind => $"{Kind}List";

    public string SingularName => Kind.ToLower();

    public string PluralName => Plural ?? $"{Kind.ToLower()}s";
}
