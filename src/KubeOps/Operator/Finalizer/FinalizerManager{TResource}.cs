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
    internal class FinalizerManager<TResource> : IFinalizerManager<TResource>
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        private readonly IKubernetesClient _client;
        private readonly IServiceProvider _services;
        private readonly ILogger<FinalizerManager<TResource>> _logger;

        public FinalizerManager(
            IKubernetesClient client,
            IServiceProvider services,
            ILogger<FinalizerManager<TResource>> logger)
        {
            _client = client;
            _services = services;
            _logger = logger;
        }

        public async Task RegisterFinalizerAsync<TFinalizer>(TResource resource)
            where TFinalizer : IResourceFinalizer<TResource>
        {
            using var scope = _services.CreateScope();
            var finalizer = scope.ServiceProvider.GetRequiredService<TFinalizer>();
            _logger.LogTrace(
                @"Try to add finalizer ""{finalizer}"" on resource ""{kind}/{name}"".",
                finalizer.Identifier,
                resource.Kind,
                resource.Name());

            if (resource.AddFinalizer(finalizer.Identifier))
            {
                _logger.LogInformation(
                    @"Added finalizer ""{finalizer}"" on resource ""{kind}/{name}"".",
                    finalizer.Identifier,
                    resource.Kind,
                    resource.Name());
            }

            await _client.Update(resource);
        }

        async Task IFinalizerManager<TResource>.FinalizeAsync(TResource resource)
        {
            using var scope = _services.CreateScope();
            var semaphore = new SemaphoreSlim(1);

            _logger.LogTrace(
                @"Try to finalize resource ""{kind}/{name}"".",
                resource.Kind,
                resource.Name());

            await Task.WhenAll(
                OperatorBuilder
                    .GetFinalizers()
                    .Select(scope.ServiceProvider.GetService)
                    .OfType<IResourceFinalizer<TResource>>()
                    .Where(finalizer => resource.HasFinalizer(finalizer.Identifier))
                    .Select(
                        finalizer => Task.Run(
                            async () =>
                            {
                                _logger.LogInformation(
                                    @"Execute finalizer ""{finalizer}"" on resource ""{kind}/{name}"".",
                                    finalizer.Identifier,
                                    resource.Kind,
                                    resource.Name());
                                await finalizer.FinalizeAsync(resource);
                                try
                                {
                                    await semaphore.WaitAsync();
                                    resource.RemoveFinalizer(finalizer.Identifier);
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            })));

            await _client.Update(resource);
            _logger.LogDebug(
                @"Finalization on resource ""{kind}/{name}"" done. Remaining finalizers: ""{remainingFinalizer}"".",
                resource.Kind,
                resource.Name(),
                string.Join(',', resource.Finalizers()));
        }
    }
}
