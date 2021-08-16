﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Finalizer
{
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

        public async Task RegisterFinalizerAsync<TFinalizer>(TEntity entity)
            where TFinalizer : IResourceFinalizer<TEntity>
        {
            var finalizer = _finalizerInstanceBuilder.BuildFinalizer<TEntity, TFinalizer>();

            await RegisterFinalizerInternalAsync(entity, finalizer);
        }

        public async Task RegisterAllFinalizersAsync(TEntity entity)
        {
            await Task.WhenAll(
                _finalizerInstanceBuilder.BuildFinalizers<TEntity>()
                    .Select(f => RegisterFinalizerInternalAsync(entity, f)));
        }

        async Task IFinalizerManager<TEntity>.FinalizeAsync(TEntity entity)
        {
            var semaphore = new SemaphoreSlim(1);

            _logger.LogTrace(
                @"Try to finalize entity ""{kind}/{name}"".",
                entity.Kind,
                entity.Name());

            await Task.WhenAll(
                _finalizerInstanceBuilder.BuildFinalizers<TEntity>()
                    .Where(finalizer => entity.HasFinalizer(finalizer.Identifier))
                    .Select(
                        finalizer => Task.Run(
                            async () =>
                            {
                                _logger.LogInformation(
                                    @"Execute finalizer ""{finalizer}"" on entity ""{kind}/{name}"".",
                                    finalizer.Identifier,
                                    entity.Kind,
                                    entity.Name());
                                await finalizer.FinalizeAsync(entity);
                                try
                                {
                                    await semaphore.WaitAsync();
                                    entity.RemoveFinalizer(finalizer.Identifier);
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            })));

            await _client.Update(entity);
            _logger.LogDebug(
                @"Finalization on entity ""{kind}/{name}"" done. Remaining finalizers: ""{remainingFinalizer}"".",
                entity.Kind,
                entity.Name(),
                string.Join(',', entity.Finalizers()));
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
            }

            await _client.Update(entity);
        }
    }
}
