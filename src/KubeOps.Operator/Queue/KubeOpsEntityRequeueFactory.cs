using k8s;
using k8s.Models;

using KubeOps.Abstractions.Queue;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Queue;

internal sealed class KubeOpsEntityRequeueFactory(IServiceProvider services)
    : IEntityRequeueFactory
{
    public EntityRequeue<TEntity> Create<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta> =>
        (entity, timeSpan) =>
        {
            var logger = services.GetService<ILogger<EntityRequeue<TEntity>>>();
            var queue = services.GetRequiredService<TimedEntityQueue<TEntity>>();

            logger?.LogTrace(
                """Requeue entity "{kind}/{name}" in {milliseconds}ms.""",
                entity.Kind,
                entity.Name(),
                timeSpan.TotalMilliseconds);

            queue.Enqueue(entity, timeSpan);
        };
}
