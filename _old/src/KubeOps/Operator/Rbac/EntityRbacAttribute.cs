namespace KubeOps.Operator.Rbac;

/// <summary>
/// Generate rbac information for a type.
/// Attach this attribute to a controller with the type reference to
/// a custom entity to define rbac needs for this given type(s).
/// </summary>
/// <example>[EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)].</example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EntityRbacAttribute : Attribute
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
    /// Flags ("list") of allowed verbs.
    ///
    /// Yaml example:
    /// "verbs: ["get", "list", "watch"]".
    /// </summary>
    public RbacVerb Verbs { get; init; }
}
