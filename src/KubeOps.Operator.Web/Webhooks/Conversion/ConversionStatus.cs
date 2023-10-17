using System.Text.Json.Serialization;

namespace KubeOps.Operator.Web.Webhooks.Conversion;

public record ConversionStatus([property: JsonPropertyName("message")]
    string? Message = null)
{
    [JsonPropertyName("status")]
    public string Status => string.IsNullOrWhiteSpace(Message) ? "Success" : "Failed";
}
