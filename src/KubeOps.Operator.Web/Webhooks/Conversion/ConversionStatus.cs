using System.Runtime.Versioning;
using System.Text.Json.Serialization;

namespace KubeOps.Operator.Web.Webhooks.Conversion;

[RequiresPreviewFeatures(
    "Conversion webhooks API is not yet stable, the way that conversion " +
    "webhooks are implemented may change in the future based on user feedback.")]
public record ConversionStatus([property: JsonPropertyName("message")]
    string? Message = null)
{
    [JsonPropertyName("status")]
    public string Status => string.IsNullOrWhiteSpace(Message) ? "Success" : "Failed";
}
