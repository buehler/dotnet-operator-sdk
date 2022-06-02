using System.Text.Json.Serialization;
using k8s;

namespace KubeOps.Operator.Webhooks;

internal sealed class AdmissionReview<TEntity> : IKubernetesObject
{
    public AdmissionReview()
    {
    }

    public AdmissionReview(AdmissionResponse response) => Response = response;

    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; set; } = "admission.k8s.io/v1";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "AdmissionReview";

    [JsonPropertyName("request")]
    public AdmissionRequest<TEntity>? Request { get; set; }

    [JsonPropertyName("response")]
    public AdmissionResponse? Response { get; set; }
}
