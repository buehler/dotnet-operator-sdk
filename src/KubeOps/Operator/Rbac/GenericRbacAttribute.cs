namespace KubeOps.Operator.Rbac;

/// <summary>
/// Generic attribute to define rbac needs for the operator.
/// This needs get generated into rbac - yaml style resources
/// for installation on a cluster.
///
/// The attribute essentially defines the role definition of kubernetes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GenericRbacAttribute : Attribute
{
    /// <summary>
    /// List of groups.
    ///
    /// Yaml example:
    /// "apiGroups: ...".
    /// </summary>
    public string[] Groups { get; init; } = { };

    /// <summary>
    /// List of resources.
    ///
    /// Yaml example:
    /// "resources: ["pods"]".
    /// </summary>
    public string[] Resources { get; init; } = { };

    /// <summary>
    /// List of urls.
    /// </summary>
    public string[] Urls { get; init; } = { };

    /// <summary>
    /// Flags ("list") of allowed verbs.
    ///
    /// Yaml example:
    /// "verbs: ["get", "list", "watch"]".
    /// </summary>
    public RbacVerb Verbs { get; init; }
}
