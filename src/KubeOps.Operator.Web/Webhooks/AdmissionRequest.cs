using System.Text.Json.Serialization;

using k8s;
using k8s.Models;

namespace KubeOps.Operator.Web.Webhooks;

/// <summary>
/// Incoming admission request for a webhook.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public sealed class AdmissionRequest<TEntity> : AdmissionReview
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    /// <summary>
    /// Admission request data.
    /// </summary>
    [JsonPropertyName("request")]
    public AdmissionRequestData<TEntity> Request { get; init; } = new();
}
