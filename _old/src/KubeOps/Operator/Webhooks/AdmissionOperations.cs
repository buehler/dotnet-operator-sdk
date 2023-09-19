namespace KubeOps.Operator.Webhooks;

/// <summary>
/// Possible admission webhook operations.
/// Those operations are watched by the kubernetes api and webhooks
/// are called according to their registered operations.
/// </summary>
[Flags]
public enum AdmissionOperations
{
    /// <summary>
    /// Default value of "none".
    /// </summary>
    None = 0,

    /// <summary>
    /// All possible operations.
    /// </summary>
    All = 1 << 0,

    /// <summary>
    /// Create operations.
    /// This is fired when an entity is new.
    /// </summary>
    Create = 1 << 1,

    /// <summary>
    /// Update operations.
    /// This is fired when an entity is updated (e.g. kubectl edit ...).
    /// </summary>
    Update = 1 << 2,

    /// <summary>
    /// Delete operations.
    /// This is fired when an entity gets deleted from the api.
    /// </summary>
    Delete = 1 << 3,
}
