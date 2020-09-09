using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Client;
using KubeOps.Operator.Entities.Extensions;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Finalizer
{
    public abstract class ResourceFinalizerBase<TResource> : IResourceFinalizer<TResource>
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        private readonly ILogger<ResourceFinalizerBase<TResource>> _logger;

        protected ResourceFinalizerBase(ILogger<ResourceFinalizerBase<TResource>> logger, IKubernetesClient client)
        {
            _logger = logger;
            Client = client;
        }

        public virtual string Identifier
        {
            get
            {
                var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TResource>();
                return $"{crd.Singular}.finalizers.{crd.Group}";
            }
        }

        protected IKubernetesClient Client { get; }

        public async Task Register(TResource resource)
        {
            if (resource.Metadata.Finalizers?.Contains(Identifier) == true)
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
            if (resource.Metadata.Finalizers?.Contains(Identifier) != true)
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
