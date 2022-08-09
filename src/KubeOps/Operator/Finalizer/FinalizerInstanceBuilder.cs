using k8s;
using k8s.Models;
using KubeOps.Operator.Builder;

namespace KubeOps.Operator.Finalizer;

internal class FinalizerInstanceBuilder : IFinalizerInstanceBuilder
{
    private readonly IComponentRegistrar _componentRegistrar;
    private readonly IServiceProvider _services;

    public FinalizerInstanceBuilder(
        IComponentRegistrar componentRegistrar,
        IServiceProvider services)
    {
        _componentRegistrar = componentRegistrar;
        _services = services;
    }

    public IResourceFinalizer<TEntity> BuildFinalizer<TEntity, TFinalizer>()
        where TEntity : IKubernetesObject<V1ObjectMeta> =>
        _componentRegistrar.FinalizerRegistrations.For<TEntity>()
            .Where(r => r.FinalizerType.IsEquivalentTo(typeof(TFinalizer)))
            .Select(r => (IResourceFinalizer<TEntity>)_services.GetRequiredService(r.FinalizerType))
            .Single();

    public IEnumerable<IResourceFinalizer<TEntity>> BuildFinalizers<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta> =>
        _componentRegistrar.FinalizerRegistrations.For<TEntity>()
            .Select(r => (IResourceFinalizer<TEntity>)_services.GetRequiredService(r.FinalizerType));
}
