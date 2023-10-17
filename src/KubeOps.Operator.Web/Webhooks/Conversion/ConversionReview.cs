using System.Text.Json.Serialization;

namespace KubeOps.Operator.Web.Webhooks.Conversion;

/// <summary>
/// Base class for conversion review requests.
/// </summary>
public abstract class ConversionReview
{
    [JsonPropertyName("apiVersion")]
    public string ApiVersion => "apiextensions.k8s.io/v1";

    [JsonPropertyName("kind")]
    public string Kind => "ConversionReview";
}
