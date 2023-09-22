namespace KubeOps.Abstractions.Entities;

#if NET
/// <summary>
/// Metadata for a given entity.
/// </summary>
/// <param name="Kind">The kind of the entity (e.g. deployment).</param>
/// <param name="Version">Version (e.g. v1 or v2-alpha).</param>
/// <param name="Group">The group in Kubernetes (e.g. "testing.dev").</param>
/// <param name="Plural">An optional plural name. Defaults to the singular name with an added "s".</param>
public record EntityMetadata(string Kind, string Version, string? Group = null, string? Plural = null)
{
    /// <summary>
    /// Kind of the entity when used in a list.
    /// </summary>
    public string ListKind => $"{Kind}List";

    /// <summary>
    /// Name of the singular entity.
    /// </summary>
    public string SingularName => Kind.ToLower();

    /// <summary>
    /// Name of the plural entity.
    /// </summary>
    public string PluralName => Plural ?? $"{Kind.ToLower()}s";
}
#else
/// <summary>
/// Metadata for a given entity.
/// </summary>
public class EntityMetadata
{
    public EntityMetadata(string kind, string version, string? group = null, string? plural = null)
    {
        Kind = kind;
        Version = version;
        Group = group;
        Plural = plural;
    }

    /// <summary>
    /// The kind of the entity (e.g. deployment).
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Version (e.g. v1 or v2-alpha).
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// The group in Kubernetes (e.g. "testing.dev").
    /// </summary>
    public string? Group { get; }

    /// <summary>
    /// An optional plural name. Defaults to the singular name with an added "s".
    /// </summary>
    public string? Plural { get; }

    /// <summary>
    /// Kind of the entity when used in a list.
    /// </summary>
    public string ListKind => $"{Kind}List";

    /// <summary>
    /// Name of the singular entity.
    /// </summary>
    public string SingularName => Kind.ToLower();

    /// <summary>
    /// Name of the plural entity.
    /// </summary>
    public string PluralName => Plural ?? $"{Kind.ToLower()}s";
}
#endif
