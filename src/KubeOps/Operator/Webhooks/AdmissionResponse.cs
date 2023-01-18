using System.Text.Json.Serialization;

namespace KubeOps.Operator.Webhooks;

internal sealed class AdmissionResponse
{
    public const string JsonPatch = "JSONPatch";

    [JsonPropertyName("uid")]
    public string Uid { get; set; } = string.Empty;

    [JsonPropertyName("allowed")]
    public bool Allowed { get; init; }

    [JsonPropertyName("status")]
    public Reason? Status { get; init; }

    [JsonPropertyName("warnings")]
    public string[] Warnings { get; init; } = Array.Empty<string>();

    [JsonPropertyName("patchType")]
    public string PatchType { get; set; } = JsonPatch;

    [JsonPropertyName("patch")]
    public string? Patch { get; set; }

    internal sealed class Reason
    {
        [JsonPropertyName("code")]
        public int Code { get; init; }

        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }
}
