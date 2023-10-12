using System.Text.Json;
using System.Text.Json.Serialization;

namespace KubeOps.Operator.Web.Webhooks;

internal sealed class AdmissionResponse : AdmissionReview
{
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    [JsonPropertyName("response")]
#if NET7_0_OR_GREATER
    public required AdmissionResponseData Response { get; init; }
#else
    public AdmissionResponseData Response { get; init; } = new();
#endif
}
