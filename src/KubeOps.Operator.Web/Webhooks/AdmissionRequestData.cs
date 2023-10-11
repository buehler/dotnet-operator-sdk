using System.Text.Json.Serialization;

using k8s;
using k8s.Models;

namespace KubeOps.Operator.Web.Webhooks;

public sealed class AdmissionRequestData<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    [JsonPropertyName("uid")]
    public string Uid { get; init; } = string.Empty;

    [JsonPropertyName("operation")]
    public string Operation { get; init; } = string.Empty;

    [JsonPropertyName("object")]
    public TEntity? Object { get; init; }

    [JsonPropertyName("oldObject")]
    public TEntity? OldObject { get; init; }

    [JsonPropertyName("dryRun")]
    public bool DryRun { get; init; }
}
