namespace KubeOps.Abstractions.Rbac;

/// <summary>
/// Generate rbac information for a type.
/// Attach this attribute to a controller with the type reference to
/// a custom entity to define rbac needs for this given type(s).
/// </summary>
/// <example>
/// Allow the operator "ALL" access to the V1TestEntity.
/// <code>
/// [EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
/// </code>
/// </example>
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
    public RbacVerb Verbs { get; init; }
}
