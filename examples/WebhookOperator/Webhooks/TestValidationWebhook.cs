using System.Reflection;
using System.Text.Json.Serialization;

using k8s;
using k8s.Models;

using Microsoft.AspNetCore.Mvc;

using WebhookOperator.Entities;

namespace WebhookOperator.Webhooks;

[ValidationWebhook(typeof(V1TestEntity))]
public class TestValidationWebhook : ValidationWebhook<V1TestEntity>
{
}

[ApiController]
public class ValidationWebhook<TEntity> : ControllerBase
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    [HttpPost]
    public async Task<IActionResult> Validate([FromBody]AdmissionReview<TEntity> request)
    {
        return Ok();
    }
}

public class ValidationWebhookAttribute : RouteAttribute
{
    public ValidationWebhookAttribute(Type entityType) : base($"/validate/{entityType.Name.ToLowerInvariant()}")
    {
    }
}

public class AdmissionReview<TEntity> : IKubernetesObject
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

public class AdmissionRequest<TEntity>
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

public class AdmissionResponse
{
    public const string JsonPatch = "JSONPatch";

    [JsonPropertyName("uid")]
    public string Uid { get; set; } = string.Empty;

    [JsonPropertyName("allowed")]
    public bool Allowed { get; init; }

    [JsonPropertyName("status")]
    public Reason? Status { get; init; }

    [JsonPropertyName("warnings")]
    public string[] Warnings { get; init; } = Array.Empty<string>();

    [JsonPropertyName("patchType")]
    public string? PatchType { get; set; }

    [JsonPropertyName("patch")]
    public string? Patch { get; set; }

    public sealed class Reason
    {
        [JsonPropertyName("code")]
        public int Code { get; init; }

        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }
}
