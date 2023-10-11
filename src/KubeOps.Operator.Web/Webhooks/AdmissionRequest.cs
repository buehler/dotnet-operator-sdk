using System.Text.Json.Serialization;

using k8s;
using k8s.Models;

namespace KubeOps.Operator.Web.Webhooks;

public sealed class AdmissionRequest<TEntity> : AdmissionReview
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    [JsonPropertyName("request")]
    public AdmissionRequestData<TEntity> Request { get; init; } = new();
}
