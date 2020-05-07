using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dos.Operator.Client;
using Dos.Operator.DependencyInjection;
using Dos.Operator.Entities;
using k8s;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dos.Operator.Finalizer
{
    public abstract class ResourceFinalizerBase<TResource> : IResourceFinalizer<TResource>
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        private readonly Lazy<IKubernetesClient> _client =
            new Lazy<IKubernetesClient>(() => DependencyInjector.Services.GetRequiredService<IKubernetesClient>());

        private readonly ILogger<ResourceFinalizerBase<TResource>> _logger;

        protected ResourceFinalizerBase()
        {
            _logger = DependencyInjector.Services.GetRequiredService<ILogger<ResourceFinalizerBase<TResource>>>();
        }

        public virtual string Identifier
        {
            get
            {
                var crd = EntityExtensions.CreateResourceDefinition<TResource>();
                return $"{crd.Singular}.finalizers.{crd.Group}";
            }
        }

        protected IKubernetesClient Client => _client.Value;

        public async Task Register(TResource resource)
        {
            if (resource.Metadata.Finalizers != null && resource.Metadata.Finalizers.Contains(Identifier))
            {
                return;
            }

            resource.Metadata.Finalizers ??= new List<string>();
            resource.Metadata.Finalizers.Add(Identifier);
            await Client.Update(resource);
            _logger.LogDebug(
                @"Registered finalizer ""{finalizer}"" on resource ""{kind}/{name}"".",
                Identifier,
                resource.Kind,
                resource.Metadata.Name);
        }

        public async Task Unregister(TResource resource)
        {
            if (resource.Metadata.Finalizers == null || !resource.Metadata.Finalizers.Contains(Identifier))
            {
                return;
            }

            resource.Metadata.Finalizers.Remove(Identifier);
            await Client.Update(resource);
            _logger.LogDebug(
                @"Unregistered finalizer ""{finalizer}"" from resource ""{kind}/{name}"".",
                Identifier,
                resource.Kind,
                resource.Metadata.Name);
        }

        public abstract Task Finalize(TResource resource);

        async Task IResourceFinalizer<TResource>.FinalizeResource(TResource resource)
        {
            try
            {
                await Finalize(resource);
                await Unregister(resource);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    @"Resource ""{kind}/{name}"" could not be finalized with finalizer ""{finalizer}"" due to an error.",
                    resource.Kind,
                    resource.Metadata.Name,
                    Identifier);
                throw;
            }
        }
    }
}
