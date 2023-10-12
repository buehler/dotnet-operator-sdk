using System.Text.Json.Serialization;

namespace KubeOps.Operator.Web.Webhooks;

internal sealed class AdmissionResponseData
{
    [JsonPropertyName("uid")]
#if NET7_0_OR_GREATER
    public required string Uid { get; init; }
#else
    public string Uid { get; init; } = string.Empty;
#endif

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
