using System.Text.Json.Serialization;

using k8s;

namespace KubeOps.Abstractions.Entities;

/// <summary>
/// Defines a custom Kubernetes entity.
/// This entity contains a spec (like <see cref="CustomKubernetesEntity{TSpec}"/>)
/// and a status (<see cref="Status"/>) which can be updated to reflect the state
/// of the entity.
/// </summary>
/// <typeparam name="TSpec">The type of the specified data.</typeparam>
/// <typeparam name="TStatus">The type of the status data.</typeparam>
public abstract class CustomKubernetesEntity<TSpec, TStatus> : CustomKubernetesEntity<TSpec>, IStatus<TStatus>
    where TSpec : new()
    where TStatus : new()
{
    /// <summary>
    /// Status object for the entity.
    /// </summary>
    // [JsonPropertyName("status")]
    public TStatus Status { get; set; } = new();
}
