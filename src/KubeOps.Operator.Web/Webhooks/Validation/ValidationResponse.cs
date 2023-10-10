using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Http;

namespace KubeOps.Operator.Web.Webhooks.Validation;

public class ValidationResponse
{
    public ValidationResponse(string requestId, ValidationResult result)
    {
        Response = new()
        {
            Uid = requestId,
            Allowed = result.Valid,
            Status = result.StatusMessage == null
                ? null
                : new ResponseObject.Reason
                {
                    Code = result.StatusCode ?? StatusCodes.Status200OK, Message = result.StatusMessage,
                },
            Warnings = result.Warnings.ToArray(),
        };
    }

    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; init; } = "admission.k8s.io/v1";

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "AdmissionReview";

    [JsonPropertyName("response")]
    public ResponseObject Response { get; init; }

    public sealed class ResponseObject
    {
        [JsonPropertyName("uid")]
        public string Uid { get; init; }

        [JsonPropertyName("allowed")]
        public bool Allowed { get; init; }

        [JsonPropertyName("status")]
        public Reason? Status { get; init; }

        [JsonPropertyName("warnings")]
        public string[] Warnings { get; init; }

        public sealed class Reason
        {
            [JsonPropertyName("code")]
            public int Code { get; init; }

            [JsonPropertyName("message")]
            public string Message { get; init; } = string.Empty;
        }
    }
}
