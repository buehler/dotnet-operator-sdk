using k8s;
using k8s.Models;
using KubeOps.KubernetesClient.Entities;

namespace KubeOps.Operator.Finalizer;

/// <summary>
/// Finalizer for a resource.
/// </summary>
/// <typeparam name="TEntity">The type of the resources (entities).</typeparam>
public interface IResourceFinalizer<in TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private const byte MaxNameLength = 63;

    /// <summary>
    /// Unique identifier for this finalizer.
    /// Defaults to `"{Typename}.{crd.Singular}.finalizers.{crd.Group}"`.
    /// </summary>
    /// <example>testentityfinalizer.test.finalizer.dev.</example>
    string Identifier
    {
        get
        {
            var crd = EntityDefinition.FromType<TEntity>();
            var finalizerName = GetType().Name.ToLowerInvariant();
            var name =
                $"{crd.Group}/{GetType().Name.ToLowerInvariant()}{(finalizerName.EndsWith("finalizer") ? string.Empty : "finalizer")}";
            return name.Length > MaxNameLength
                ? name[..MaxNameLength]
                : name;
        }
    }

    /// <summary>
    /// Finalize a resource that is pending for deletion.
    /// </summary>
    /// <param name="entity">The kubernetes entity that needs to be finalized.</param>
    /// <returns>A task for when the operation is done.</returns>
    Task FinalizeAsync(TEntity entity);
}
