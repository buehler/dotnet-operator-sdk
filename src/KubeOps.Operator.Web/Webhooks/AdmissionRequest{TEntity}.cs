using System.Text.Json.Serialization;

namespace KubeOps.Operator.Web.Webhooks;

public class AdmissionRequest<TEntity>
{
    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; set; } = "admission.k8s.io/v1";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "AdmissionReview";

    [JsonPropertyName("request")]
    public RequestObject Request { get; set; } = new();

    public class RequestObject
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
}
