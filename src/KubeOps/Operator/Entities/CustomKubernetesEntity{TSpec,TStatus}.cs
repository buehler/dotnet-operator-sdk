using k8s;

namespace KubeOps.Operator.Entities;

/// <summary>
/// Defines a custom kubernetes entity which can be used in finalizers and controllers.
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
    public TStatus Status { get; set; } = new();
}
