using k8s;
using k8s.Models;
using KubeOps.KubernetesClient.Entities;
using Prometheus;

namespace KubeOps.Operator.DevOps;

internal class ResourceCacheMetrics<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private static readonly string[] Labels = { "operator", "kind", "group", "version", "scope", };

    public ResourceCacheMetrics(OperatorSettings settings)
    {
        var crd = EntityDefinition.FromType<TEntity>();
        var labelValues = new[] { settings.Name, crd.Kind, crd.Group, crd.Version, crd.Scope.ToString(), };

        CachedItemsSummary = Metrics
            .CreateSummary(
                "operator_resource_cached_items_size",
                "Summary of the cached items count over the last 10 minutes",
                Labels)
            .WithLabels(labelValues);

        CachedItemsSize = Metrics
            .CreateGauge(
                "operator_resource_cached_items_count",
                "Total number of cached items in this resource cache",
                Labels)
            .WithLabels(labelValues);
    }

    public Summary.Child CachedItemsSummary { get; }

    public Gauge.Child CachedItemsSize { get; }
}
