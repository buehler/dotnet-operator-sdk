namespace KubeOps.Operator.Webhooks;

/// <summary>
/// Result that should be returned to kubernetes.
/// This result gets translated to a result object that is then transmitted to kubernetes.
/// Describes results of an <see cref="IMutationWebhook{TEntity}"/>.
/// </summary>
public sealed class MutationResult : AdmissionResult
{
    internal object? ModifiedObject { get; init; }

    /// <summary>
    /// Utility method that creates a return value that indicates that no changes must be applied.
    /// </summary>
    /// <returns>A <see cref="MutationResult"/> with no changes.</returns>
    public static MutationResult NoChanges() => new();

    /// <summary>
    /// Utility method that creates a return value that indicates that changes were made
    /// to the object that must be patched.
    /// This creates a json patch (<a href="http://jsonpatch.com/">jsonpatch.com</a>)
    /// that describes the diff from the original object to the modified object.
    /// </summary>
    /// <param name="modifiedEntity">The modified object.</param>
    /// <param name="warnings">
    /// An optional list of warnings/messages given back to the user.
    /// This could contain a reason why an object was mutated.
    /// </param>
    /// <returns>A <see cref="MutationResult"/> with a modified object.</returns>
    public static MutationResult Modified(object modifiedEntity, params string[] warnings) => new()
    {
        ModifiedObject = modifiedEntity, Warnings = warnings,
    };

    internal static MutationResult Fail(int statusCode, string statusMessage) => new()
    {
        Valid = false, StatusCode = statusCode, StatusMessage = statusMessage,
    };
}
