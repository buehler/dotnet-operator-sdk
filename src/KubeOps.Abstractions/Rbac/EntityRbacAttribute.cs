namespace KubeOps.Abstractions.Rbac;

/// <summary>
/// Generate rbac information for a type.
/// Attach this attribute to a controller with the type reference to
/// a custom entity to define rbac needs for this given type(s).
/// </summary>
/// <example>[EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)].</example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EntityRbacAttribute : RbacAttribute
{
    public EntityRbacAttribute(params Type[] entities)
    {
        Entities = entities;
    }

    /// <summary>
    /// List of types that this rbac verbs are valid.
    /// </summary>
    public IEnumerable<Type> Entities { get; }

    /// <summary>
    /// <para>Flags ("list") of allowed verbs.</para>
    /// <para>
    /// Yaml example:
    /// "verbs: ["get", "list", "watch"]".
    /// </para>
    /// </summary>
#if NETSTANDARD
    public RbacVerb Verbs { get; set; }
#else
    public RbacVerb Verbs { get; init; }
#endif
}
