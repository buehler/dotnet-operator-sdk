using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Entities;

public class DefaultEntityLabelSelector<TEntity> : IEntityLabelSelector<TEntity, DefaultEntityLabelSelector<TEntity>>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    public ValueTask<string?> GetLabelSelectorAsync(CancellationToken cancellationToken) => ValueTask.FromResult<string?>(null);
}
