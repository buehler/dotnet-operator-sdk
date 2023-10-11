using System.Text.Json.Serialization;

namespace KubeOps.Operator.Web.Webhooks;

/// <summary>
/// Base class for admission review requests.
/// </summary>
public abstract class AdmissionReview
{
    [JsonPropertyName("apiVersion")]
    public string ApiVersion => "admission.k8s.io/v1";

    [JsonPropertyName("kind")]
    public string Kind => "AdmissionReview";
}
