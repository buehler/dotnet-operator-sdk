﻿using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Queue;

/// <summary>
/// <para>Injectable delegate for requeueing entities.</para>
/// <para>
/// Use this delegate when you need to pro-actively reconcile an entity after a
/// certain amount of time. This is useful if you want to check your entities
/// periodically.
/// </para>
/// <para>
/// After the timeout is reached, the entity is fetched
/// from the API and passed to the controller for reconciliation.
/// If the entity was deleted in the meantime, the controller will not be called.
/// </para>
/// <para>
/// If the entity gets modified while the timeout is running, the timer
/// is canceled and restarted, if another requeue is requested.
/// </para>
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <param name="entity">The instance of the entity that should be requeued.</param>
/// <param name="requeueIn">The time to wait before another reconcile loop is fired.</param>
/// <example>
/// Use the requeue delegate to repeatedly reconcile an entity after 5 seconds.
/// <code>
/// [EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
/// public class V1TestEntityController : IEntityController&lt;V1TestEntity&gt;
/// {
///     private readonly EntityRequeue&lt;V1TestEntity&gt; _requeue;
///
///     public V1TestEntityController(EntityRequeue&lt;V1TestEntity&gt; requeue)
///     {
///         _requeue = requeue;
///     }
///
///     public async Task ReconcileAsync(V1TestEntity entity)
///     {
///         _requeue(entity, TimeSpan.FromSeconds(5));
///     }
/// }
/// </code>
/// </example>
public delegate void EntityRequeue<TEntity>(TEntity entity, TimeSpan requeueIn)
    where TEntity : IKubernetesObject<V1ObjectMeta>;
