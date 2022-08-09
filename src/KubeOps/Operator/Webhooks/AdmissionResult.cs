namespace KubeOps.Operator.Webhooks;

/// <summary>
/// Definition of an admission result for <see cref="IAdmissionWebhook{TEntity,TResult}"/>.
/// </summary>
public class AdmissionResult
{
    internal AdmissionResult()
    {
    }

    /// <summary>
    /// Determines if the result of the admission webhook is
    /// either valid or invalid.
    /// </summary>
    public bool Valid { get; init; } = true;

    /// <summary>
    /// Provides a (http) status code for Kubernetes for an admission result.
    /// This field may be used to provide additional information to the
    /// user.
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// Provide an additional status message for the admission result.
    /// This provides additional information for the user.
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Despite being "valid", the validation can add a list of warnings to the user.
    /// If this is not yet supported by the cluster, the field is ignored.
    /// Warnings may contain up to 256 characters but they should be limited to 120 characters.
    /// If more than 4096 characters are submitted, additional messages are ignored.
    /// </summary>
    public IList<string> Warnings { get; init; } = new List<string>();

    internal static TResult NotImplemented<TResult>()
        where TResult : AdmissionResult, new() => new()
    {
        Valid = false,
        StatusCode = StatusCodes.Status501NotImplemented,
        StatusMessage = "The method is not implemented.",
    };

    internal static AdmissionResponse InternalServerError()
        => new()
        {
            Allowed = false,
            Status = new()
            {
                Code = StatusCodes.Status500InternalServerError,
                Message = "There was an internal server error.",
            },
        };
}
