using System.Runtime.Versioning;
using System.Text.Json.Serialization;

namespace KubeOps.Operator.Web.Webhooks.Conversion;

/// <summary>
/// Base class for conversion review requests.
/// </summary>
[RequiresPreviewFeatures(
    "Conversion webhooks API is not yet stable, the way that conversion " +
    "webhooks are implemented may change in the future based on user feedback.")]
public abstract class ConversionReview
{
    [JsonPropertyName("apiVersion")]
    public string ApiVersion => "apiextensions.k8s.io/v1";

    [JsonPropertyName("kind")]
    public string Kind => "ConversionReview";
}
