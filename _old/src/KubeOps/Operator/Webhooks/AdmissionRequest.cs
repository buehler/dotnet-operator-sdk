using System.Text.Json.Serialization;

namespace KubeOps.Operator.Webhooks;

internal sealed class AdmissionRequest<TEntity>
{
    [JsonPropertyName("uid")]
    public string Uid { get; init; } = string.Empty;

    [JsonPropertyName("operation")]
    public string Operation { get; init; } = string.Empty;

    [JsonPropertyName("object")]
    public TEntity? Object { get; set; }

    [JsonPropertyName("oldObject")]
    public TEntity? OldObject { get; set; }

    [JsonPropertyName("dryRun")]
    public bool DryRun { get; set; }
}
