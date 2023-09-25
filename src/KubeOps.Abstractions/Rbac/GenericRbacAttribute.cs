namespace KubeOps.Abstractions.Rbac;

/// <summary>
/// <para>
/// Generic attribute to define rbac needs for the operator.
/// This needs get generated into rbac - yaml style resources
/// for installation on a cluster.
/// </para>
/// <para>The attribute essentially defines the role definition of kubernetes.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GenericRbacAttribute : RbacAttribute
{
    /// <summary>
    /// <para>List of groups.</para>
    /// <para>
    /// Yaml example:
    /// "apiGroups: ...".
    /// </para>
    /// </summary>
#if NETSTANDARD
    public string[] Groups { get; set; } = { };
#else
    public string[] Groups { get; init; } = { };
#endif

    /// <summary>
    /// <para>List of resources.</para>
    /// <para>
    /// Yaml example:
    /// "resources: ["pods"]".
    /// </para>
    /// </summary>
#if NETSTANDARD
    public string[] Resources { get; set; } = { };
#else
    public string[] Resources { get; init; } = { };
#endif

    /// <summary>
    /// List of urls.
    /// </summary>
#if NETSTANDARD
    public string[] Urls { get; set; } = { };
#else
    public string[] Urls { get; init; } = { };
#endif

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
