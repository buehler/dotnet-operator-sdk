using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using KubeOps.Operator.Web.Webhooks.Admission;

namespace KubeOps.Operator.Web.Webhooks.Conversion;

/// <summary>
/// Incoming conversion request for a webhook.
/// </summary>
public sealed class ConversionRequest : ConversionReview
{
    /// <summary>
    /// Conversion request data.
    /// </summary>
    [JsonPropertyName("request")]
    public ConversionRequestData Request { get; init; } = new();

    public sealed class ConversionRequestData
    {
        /// <summary>
        /// The unique ID of the conversion request.
        /// </summary>
        [JsonPropertyName("uid")]
        public string Uid { get; init; } = string.Empty;

        [JsonPropertyName("desiredAPIVersion")]
        public string DesiredApiVersion { get; init; } = string.Empty;

        /// <summary>
        /// TODO
        /// </summary>
        [JsonPropertyName("objects")]
        public JsonNode[] Objects { get; init; } = Array.Empty<JsonNode>();
    }
}
