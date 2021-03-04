using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace KubeOps.Operator.Webhooks
{
    /// <summary>
    /// Definition of an admission result for <see cref="IAdmissionWebhook{TEntity,TResult}"/>.
    /// </summary>
    public class AdmissionResult
    {
        internal AdmissionResult()
        {
        }

        internal bool Valid { get; init; } = true;

        /// <summary>
        /// Despite being "valid", the validation can add a list of warnings to the user.
        /// If this is not yet supported by the cluster, the field is ignored.
        /// Warnings may contain up to 256 characters but they should be limited to 120 characters.
        /// If more than 4096 characters are submitted, additional messages are ignored.
        /// </summary>
        internal IList<string> Warnings { get; init; } = new List<string>();

        internal int? StatusCode { get; init; }

        internal string? StatusMessage { get; init; }

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
}
