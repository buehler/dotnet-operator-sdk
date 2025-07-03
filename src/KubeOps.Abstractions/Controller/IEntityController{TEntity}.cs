using k8s;
using k8s.Models;
using KubeOps.Abstractions.Entities;

namespace KubeOps.Abstractions.Controller;

/// <summary>
/// Generic entity controller. The controller manages the reconcile loop
/// for a given entity type with a selector.
/// </summary>
/// <typeparam name="TEntity">The type of the Kubernetes entity.</typeparam>
/// <typeparam name="TSelector">The type of the label selector for the entity.</typeparam>
/// <example>
/// Simple example controller that just logs the entity.
/// <code>
/// public class V1TestEntityController : IEntityController&lt;V1TestEntity, DefaultEntityLabelSelector&lt;V1TestEntity&gt;&gt;
/// {
///     private readonly ILogger&lt;V1TestEntityController&gt; _logger;
///
///     public V1TestEntityController(
///         ILogger&lt;V1TestEntityController&gt; logger)
///     {
///         _logger = logger;
///     }
///
///     public async Task ReconcileAsync(V1TestEntity entity, CancellationToken token)
///     {
///         _logger.LogInformation("Reconciling entity {Entity}.", entity);
///     }
///
///     public async Task DeletedAsync(V1TestEntity entity, CancellationToken token)
///     {
///         _logger.LogInformation("Deleting entity {Entity}.", entity);
///     }
/// }
/// </code>
/// </example>
public interface IEntityController<TEntity, in TSelector>
    where TEntity : IKubernetesObject<V1ObjectMeta>
    where TSelector : class, IEntityLabelSelector<TEntity, TSelector>
{
    /// <summary>
    /// Called for `added` and `modified` events from the watcher.
    /// </summary>
    /// <param name="entity">The entity that fired the reconcile event.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the reconciliation is done.</returns>
    Task ReconcileAsync(TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// Called for `delete` events for a given entity.
    /// </summary>
    /// <param name="entity">The entity that fired the deleted event.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that completes, when the reconciliation is done.
    /// </returns>
    Task DeletedAsync(TEntity entity, CancellationToken cancellationToken);
}

/// <summary>
/// Generic entity controller. The controller manages the reconcile loop
/// for a given entity type. This is a compatibility interface that inherits
/// from the new two-parameter interface with a default selector.
/// </summary>
/// <typeparam name="TEntity">The type of the Kubernetes entity.</typeparam>
/// <example>
/// Simple example controller that just logs the entity.
/// <code>
/// public class V1TestEntityController : IEntityController&lt;V1TestEntity&gt;
/// {
///     private readonly ILogger&lt;V1TestEntityController&gt; _logger;
///
///     public V1TestEntityController(
///         ILogger&lt;V1TestEntityController&gt; logger)
///     {
///         _logger = logger;
///     }
///
///     public async Task ReconcileAsync(V1TestEntity entity, CancellationToken token)
///     {
///         _logger.LogInformation("Reconciling entity {Entity}.", entity);
///     }
///
///     public async Task DeletedAsync(V1TestEntity entity, CancellationToken token)
///     {
///         _logger.LogInformation("Deleting entity {Entity}.", entity);
///     }
/// }
/// </code>
/// </example>
public interface IEntityController<TEntity> : IEntityController<TEntity, DefaultEntityLabelSelector<TEntity>>
    where TEntity : IKubernetesObject<V1ObjectMeta>;
