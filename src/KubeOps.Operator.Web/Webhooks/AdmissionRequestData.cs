using System.Text.Json.Serialization;

using k8s;
using k8s.Models;

namespace KubeOps.Operator.Web.Webhooks;

/// <summary>
/// Data for an incoming admission request.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public sealed class AdmissionRequestData<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    /// <summary>
    /// The unique ID of the admission request.
    /// </summary>
    [JsonPropertyName("uid")]
    public string Uid { get; init; } = string.Empty;

    /// <summary>
    /// The operation that is used.
    /// Valid values are: "CREATE", "UPDATE", "DELETE".
    /// "CONNECT" does exist, but is not supported by the operator-sdk.
    /// </summary>
    [JsonPropertyName("operation")]
    public string Operation { get; init; } = string.Empty;

    /// <summary>
    /// If set, the object that is passed to the webhook.
    /// This is set in CREATE and UPDATE operations.
    /// </summary>
    [JsonPropertyName("object")]
    public TEntity? Object { get; init; }

    /// <summary>
    /// If set, the old object that is passed to the webhook.
    /// This is set in UPDATE and DELETE operations.
    /// </summary>
    [JsonPropertyName("oldObject")]
    public TEntity? OldObject { get; init; }

    /// <summary>
    /// A flag to indicate if the API was called with the "dryRun" flag.
    /// </summary>
    [JsonPropertyName("dryRun")]
    public bool DryRun { get; init; }
}
