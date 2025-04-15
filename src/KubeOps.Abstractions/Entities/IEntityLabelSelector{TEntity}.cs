using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Entities;

public interface IEntityLabelSelector<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    ValueTask<string?> GetLabelSelectorAsync(CancellationToken cancellationToken);
}
