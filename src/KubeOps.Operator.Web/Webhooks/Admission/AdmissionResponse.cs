using System.Text.Json.Serialization;

namespace KubeOps.Operator.Web.Webhooks.Admission;

internal sealed class AdmissionResponse : AdmissionReview
{
    [JsonPropertyName("response")]
    public AdmissionResponseData Response { get; init; } = new();

    internal sealed class AdmissionResponseData
    {
        [JsonPropertyName("uid")]
        public string Uid { get; init; } = string.Empty;

        [JsonPropertyName("allowed")]
        public bool Allowed { get; init; }

        [JsonPropertyName("status")]
        public AdmissionStatus? Status { get; init; }

        [JsonPropertyName("warnings")]
        public string[]? Warnings { get; init; }

        [JsonPropertyName("patch")]
        public string? Patch { get; init; }

        [JsonPropertyName("patchType")]
        public string? PatchType { get; init; }
    }
}
