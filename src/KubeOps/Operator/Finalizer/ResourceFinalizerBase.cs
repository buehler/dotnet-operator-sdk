using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Entities.Extensions;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Finalizer
{
    /// <summary>
    /// Resource finalizer base. Inherit this class to create a resource finalizer.
    /// The finalizer is registered with <see cref="IOperatorBuilder.AddFinalizer{TFinalizer}"/>.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource.</typeparam>
    public abstract class ResourceFinalizerBase<TResource> : IResourceFinalizer<TResource>
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        private readonly ILogger<ResourceFinalizerBase<TResource>> _logger;

        protected ResourceFinalizerBase(ILogger<ResourceFinalizerBase<TResource>> logger, IKubernetesClient client)
        {
            _logger = logger;
            Client = client;
        }

        /// <summary>
        /// The unique identifier for the finalizer.
        /// Defaults to the singular name of the CRD with the group attached.
        /// </summary>
        /// <example>testentity.finalizers.dev.</example>
        public virtual string Identifier
        {
            get
            {
                var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TResource>();
                return $"{crd.Singular}.finalizers.{crd.Group}";
            }
        }

        /// <summary>
        /// The instance of the kubernetes client for querying the cluster.
        /// </summary>
        protected IKubernetesClient Client { get; }

        /// <summary>
        /// Register the finalizer on a resource.
        /// This essentially checks if the list of finalizers already contains the
        /// identifier. If not: attach it and update the resource.
        /// </summary>
        /// <param name="resource">The resource to attach the finalizer to.</param>
        /// <returns>A task that resolves when everything is done.</returns>
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

        /// <summary>
        /// Unregister the finalizer from a given resource.
        /// </summary>
        /// <param name="resource">The resource that the finalizer should be removed.</param>
        /// <returns>A task that resolves when everything is done.</returns>
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

        /// <summary>
        /// The finalize method. This is called when a resource is in the
        /// process of being deleted. When the <see cref="Identifier"/>
        /// is in the list of finalizers, this method will be called.
        /// </summary>
        /// <param name="resource">The resource that is being finalized.</param>
        /// <returns>A task that resolves when everything is done.</returns>
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
