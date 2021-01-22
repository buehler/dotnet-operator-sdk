using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace KubeOps.Operator.Webhooks
{
    /// <summary>
    /// Result that should be returned to kubernetes.
    /// This result gets translated to a result object that is then transmitted to kubernetes.
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>
        /// Indicates if the result of the validation is "valid" or "invalid".
        /// </summary>
        public bool Valid { get; init; } = true;

        /// <summary>
        /// Despite being "valid", the validation can add a list of warnings to the user.
        /// If this is not yet supported by the cluster, the field is ignored.
        /// Warnings may contain up to 256 characters but they should be limited to 120 characters.
        /// If more than 4096 characters are submitted, additional messages are ignored.
        /// </summary>
        public IList<string> Warnings { get; init; } = new List<string>();

        /// <summary>
        /// If "invalid", the validator can customize the returned HTTP status code here.
        /// </summary>
        public int? StatusCode { get; init; }

        /// <summary>
        /// If "invalid", the validator can customize the returned error message here.
        /// </summary>
        public string? StatusMessage { get; init; }

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
            Valid = false,
            StatusCode = statusCode,
            StatusMessage = statusMessage,
        };

        internal static ValidationResult NotImplemented() => new()
        {
            Valid = false,
            StatusCode = StatusCodes.Status501NotImplemented,
            StatusMessage = "The validation method is not implemented.",
        };

        internal AdmissionResponse CreateResponse(string requestUid) => new()
        {
            Uid = requestUid,
            Allowed = Valid,
            Status = StatusMessage == null
                ? null
                : new AdmissionResponse.Reason
                {
                    Code = StatusCode ?? 0,
                    Message = StatusMessage,
                },
            Warnings = Warnings.ToArray(),
        };
    }
}
