using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Entities;

// This is the same pattern used by Microsoft on ILogger<T>.
// An alternative would be to use a KeyedSingleton when registering this however that's only valid from .NET 8 and above.
// Other methods are far less elegant
#pragma warning disable S2326
public interface IEntityLabelSelector<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    ValueTask<string?> GetLabelSelectorAsync(CancellationToken cancellationToken);
}
#pragma warning restore S2326
