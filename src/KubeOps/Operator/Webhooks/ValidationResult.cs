namespace KubeOps.Operator.Webhooks;

/// <summary>
/// Result that should be returned to kubernetes.
/// This result gets translated to a result object that is then transmitted to kubernetes.
/// Describes results of an <see cref="IValidationWebhook{TEntity}"/>.
/// </summary>
public sealed class ValidationResult : AdmissionResult
{
    /// <summary>
    /// Utility method that creates a successful (valid) result with an optional
    /// list of warnings.
    /// </summary>
    /// <param name="warnings">An optional list of warnings to append.</param>
    /// <returns>A valid <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Success(params string[] warnings) => new() { Warnings = warnings };

    /// <summary>
    /// Utility method that creates a fail result without any further information.
    /// </summary>
    /// <returns>An invalid <see cref="ValidationResult"/>.</returns>
    public static ValidationResult Fail() => new() { Valid = false };

    /// <summary>
    /// Utility method that creates a fail result with a customized http status code
    /// and status message.
    /// </summary>
    /// <param name="statusCode">The custom http-status-code.</param>
    /// <param name="statusMessage">The custom status-message.</param>
    /// <returns>An invalid <see cref="ValidationResult"/> with a custom status-code and status-message.</returns>
    public static ValidationResult Fail(int statusCode, string statusMessage) => new()
    {
        Valid = false, StatusCode = statusCode, StatusMessage = statusMessage,
    };
}
