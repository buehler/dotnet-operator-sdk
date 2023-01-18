using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;

namespace KubeOps.Operator.Finalizer;

internal class FinalizerManager<TEntity> : IFinalizerManager<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private readonly IFinalizerInstanceBuilder _finalizerInstanceBuilder;
    private readonly IKubernetesClient _client;
    private readonly ILogger<FinalizerManager<TEntity>> _logger;

    public FinalizerManager(
        IKubernetesClient client,
        ILogger<FinalizerManager<TEntity>> logger,
        IFinalizerInstanceBuilder finalizerInstanceBuilder)
    {
        _client = client;
        _logger = logger;
        _finalizerInstanceBuilder = finalizerInstanceBuilder;
    }

    public Task RegisterFinalizerAsync<TFinalizer>(TEntity entity)
        where TFinalizer : IResourceFinalizer<TEntity>
        => RegisterFinalizerInternalAsync(entity, _finalizerInstanceBuilder.BuildFinalizer<TEntity, TFinalizer>());

    public async Task RegisterAllFinalizersAsync(TEntity entity)
    {
        await Task.WhenAll(
            _finalizerInstanceBuilder.BuildFinalizers<TEntity>()
                .Select(f => RegisterFinalizerInternalAsync(entity, f)));
    }

    public async Task RemoveFinalizerAsync<TFinalizer>(TEntity entity)
        where TFinalizer : IResourceFinalizer<TEntity>
    {
        var finalizer = _finalizerInstanceBuilder.BuildFinalizer<TEntity, TFinalizer>();

        _logger.LogTrace(
            @"Try to add finalizer ""{finalizer}"" on entity ""{kind}/{name}"".",
            finalizer.Identifier,
            entity.Kind,
            entity.Name());

        if (entity.RemoveFinalizer(finalizer.Identifier))
        {
            _logger.LogInformation(
                @"Removed finalizer ""{finalizer}"" on entity ""{kind}/{name}"".",
                finalizer.Identifier,
                entity.Kind,
                entity.Name());
            await _client.Update(entity);
        }
    }

    async Task IFinalizerManager<TEntity>.FinalizeAsync(TEntity entity)
    {
        var semaphore = new SemaphoreSlim(1);

        _logger.LogTrace(
            @"Try to finalize entity ""{kind}/{name}"".",
            entity.Kind,
            entity.Name());

        var finalizerCalled = false;
        await Task.WhenAll(
            _finalizerInstanceBuilder.BuildFinalizers<TEntity>()
                .Where(finalizer => entity.HasFinalizer(finalizer.Identifier))
                .Select(
                    finalizer => Task.Run(
                        async () =>
                        {
                            finalizerCalled = true;
                            _logger.LogInformation(
                                @"Execute finalizer ""{finalizer}"" on entity ""{kind}/{name}"".",
                                finalizer.Identifier,
                                entity.Kind,
                                entity.Name());
                            try
                            {
                                await semaphore.WaitAsync();
                                await finalizer.FinalizeAsync(entity);
                                entity.RemoveFinalizer(finalizer.Identifier);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        })));

        if (finalizerCalled)
        {
            await _client.Update(entity);
        }

        _logger.LogDebug(
            @"Finalization on entity ""{kind}/{name}"" done. Remaining finalizers: ""{remainingFinalizer}"".",
            entity.Kind,
            entity.Name(),
            string.Join(',', entity.Finalizers() ?? Array.Empty<string>()));
    }

    private async Task RegisterFinalizerInternalAsync<TFinalizer>(TEntity entity, TFinalizer finalizer)
        where TFinalizer : IResourceFinalizer<TEntity>
    {
        _logger.LogTrace(
            @"Try to add finalizer ""{finalizer}"" on entity ""{kind}/{name}"".",
            finalizer.Identifier,
            entity.Kind,
            entity.Name());

        if (entity.AddFinalizer(finalizer.Identifier))
        {
            _logger.LogInformation(
                @"Added finalizer ""{finalizer}"" on entity ""{kind}/{name}"".",
                finalizer.Identifier,
                entity.Kind,
                entity.Name());
            await _client.Update(entity);
        }
    }
}
