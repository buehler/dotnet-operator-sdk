using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using KubeOps.Operator.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Finalizer
{
    internal class FinalizerManager<TEntity> : IFinalizerManager<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private readonly IComponentRegistrar _componentRegistrar;
        private readonly IKubernetesClient _client;
        private readonly IServiceProvider _services;
        private readonly ILogger<FinalizerManager<TEntity>> _logger;

        public FinalizerManager(
            IKubernetesClient client,
            IServiceProvider services,
            ILogger<FinalizerManager<TEntity>> logger,
            IComponentRegistrar componentRegistrar)
        {
            _client = client;
            _services = services;
            _logger = logger;
            _componentRegistrar = componentRegistrar;
        }

        public async Task RegisterFinalizerAsync<TFinalizer>(TEntity entity)
            where TFinalizer : IResourceFinalizer<TEntity>
        {
            using var scope = _services.CreateScope();
            var finalizer = scope.ServiceProvider.GetRequiredService<TFinalizer>();
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

        public async Task RegisterAllFinalizersAsync(TEntity entity)
        {
            var registerFinalizerMethod = GetType().GetMethod(nameof(RegisterFinalizerAsync));

            if (registerFinalizerMethod is null)
            {
                return;
            }

            foreach (var registration in _componentRegistrar.FinalizerRegistrations.For<TEntity>())
            {
                if (registerFinalizerMethod.MakeGenericMethod(registration.FinalizerType).Invoke(this, new object[] { entity }) is Task task)
                {
                    await task;
                }
            }
        }

        async Task IFinalizerManager<TEntity>.FinalizeAsync(TEntity entity)
        {
            using var scope = _services.CreateScope();
            var semaphore = new SemaphoreSlim(1);

            _logger.LogTrace(
                @"Try to finalize entity ""{kind}/{name}"".",
                entity.Kind,
                entity.Name());

            await Task.WhenAll(
                _componentRegistrar.FinalizerRegistrations.For<TEntity>()
                    .Select(r => r.FinalizerType)
                    .Select(scope.ServiceProvider.GetService)
                    .OfType<IResourceFinalizer<TEntity>>()
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
    }
}
