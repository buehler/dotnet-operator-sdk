namespace KubeOps.Operator.Rbac;

/// <summary>
/// List of possible rbac verbs.
/// </summary>
[Flags]
public enum RbacVerb
{
    /// <summary>
    /// No permissions on the resource.
    /// </summary>
    None = 0,

    /// <summary>
    /// All possible permissions.
    /// </summary>
    All = 1 << 0,

    /// <summary>
    /// Retrieve the resource from the api.
    /// </summary>
    Get = 1 << 1,

    /// <summary>
    /// List resources on the api.
    /// </summary>
    List = 1 << 2,

    /// <summary>
    /// Watch for events on resources.
    /// </summary>
    Watch = 1 << 3,

    /// <summary>
    /// Create new instances of the resource.
    /// </summary>
    Create = 1 << 4,

    /// <summary>
    /// Update existing resources.
    /// </summary>
    Update = 1 << 5,

    /// <summary>
    /// Patch resources.
    /// </summary>
    Patch = 1 << 6,

    /// <summary>
    /// Delete resources on the api.
    /// </summary>
    Delete = 1 << 7,
}
