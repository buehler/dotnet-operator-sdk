using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Finalizer;

/// <summary>
/// <para>
/// Injectable delegate for finalizers. This delegate is used to attach a finalizer
/// with its identifier to an entity. When injected, simply call the delegate with
/// the entity to attach the finalizer.
/// </para>
/// <para>
/// As with other (possibly) mutating calls, use the returned entity for further
/// modification and Kubernetes client interactions, since the resource version
/// is updated each time the entity is modified.
/// </para>
/// </summary>
/// <typeparam name="TImplementation">The type of the entity finalizer.</typeparam>
/// <typeparam name="TEntity">The type of the Kubernetes entity.</typeparam>
/// <param name="entity">The instance of the entity, that the finalizer is attached if needed.</param>
/// <returns>A <see cref="Task"/> that resolves when the finalizer was attached.</returns>
/// <example>
/// <code>
/// [EntityRbac(typeof(V1TestEntity), Verbs = RbacVerb.All)]
/// public class V1TestEntityController : IEntityController&lt;V1TestEntity&gt;
/// {
///     private readonly EntityFinalizerAttacher&lt;FinalizerOne, V1TestEntity&gt; _finalizer1;
///
///     public V1TestEntityController(
///         EntityFinalizerAttacher&lt;FinalizerOne, V1TestEntity&gt; finalizer1) => _finalizer1 = finalizer1;
///
///     public async Task ReconcileAsync(V1TestEntity entity)
///     {
///         entity = await _finalizer1(entity);
///     }
/// }
/// </code>
/// </example>
public delegate Task<TEntity> EntityFinalizerAttacher<TImplementation, TEntity>(TEntity entity)
    where TImplementation : IEntityFinalizer<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>;
