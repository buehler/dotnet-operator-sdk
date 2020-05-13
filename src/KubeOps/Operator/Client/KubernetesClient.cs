using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Client.LabelSelectors;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Extensions;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;

namespace KubeOps.Operator.Client
{
    internal class KubernetesClient : IKubernetesClient
    {
        public KubernetesClient(IKubernetes apiClient)
        {
            ApiClient = apiClient;
        }

        public IKubernetes ApiClient { get; }

        public async Task<TResource?> Get<TResource>(
            string name,
            string? @namespace = null)
            where TResource : class, IKubernetesObject<V1ObjectMeta>
        {
            var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TResource>();
            try
            {
                var result = await (string.IsNullOrWhiteSpace(@namespace)
                    ? ApiClient.GetClusterCustomObjectAsync(crd.Group, crd.Version, crd.Plural, name)
                    : ApiClient.GetNamespacedCustomObjectAsync(
                        crd.Group,
                        crd.Version,
                        @namespace,
                        crd.Plural,
                        name)) as JObject;
                return result?.ToObject<TResource>();
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IList<TResource>> List<TResource>(
            string? @namespace = null,
            string? labelSelector = null)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TResource>();
            var result = await (string.IsNullOrWhiteSpace(@namespace)
                ? ApiClient.ListClusterCustomObjectAsync(
                    crd.Group,
                    crd.Version,
                    crd.Plural,
                    labelSelector: labelSelector)
                : ApiClient.ListNamespacedCustomObjectAsync(
                    crd.Group,
                    crd.Version,
                    @namespace,
                    crd.Plural,
                    labelSelector: labelSelector)) as JObject;

            var resources = result?.ToObject<EntityList<TResource>>();
            if (resources == null)
            {
                throw new ArgumentException("Could not parse result");
            }

            return resources.Items;
        }

        public Task<IList<TResource>> List<TResource>(
            string? @namespace = null,
            params ILabelSelector[] labelSelectors)
            where TResource : IKubernetesObject<V1ObjectMeta> =>
            List<TResource>(@namespace, string.Join(',', labelSelectors.Select(l => l.ToExpression())));

        public async Task<TResource> Save<TResource>(TResource resource)
            where TResource : class, IKubernetesObject<V1ObjectMeta>
        {
            var serverResource = await Get<TResource>(resource.Metadata.Name, resource.Metadata.NamespaceProperty);
            if (serverResource == null)
            {
                return await Create(resource);
            }

            resource.Metadata.Uid = serverResource.Metadata.Uid;
            resource.Metadata.ResourceVersion = serverResource.Metadata.ResourceVersion;

            return await Update(resource);
        }

        public async Task<TResource> Create<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var crd = resource.CreateResourceDefinition();
            var result = await (string.IsNullOrWhiteSpace(resource.Metadata.NamespaceProperty)
                ? ApiClient.CreateClusterCustomObjectAsync(
                    resource,
                    crd.Group,
                    crd.Version,
                    crd.Plural)
                : ApiClient.CreateNamespacedCustomObjectAsync(
                    resource,
                    crd.Group,
                    crd.Version,
                    resource.Metadata.NamespaceProperty,
                    crd.Plural)) as JObject;

            if (result?.ToObject(resource.GetType()) is TResource parsed)
            {
                resource.Metadata.ResourceVersion = parsed.Metadata.ResourceVersion;
                return parsed;
            }

            throw new ArgumentException("Could not parse result");
        }

        public async Task<TResource> Update<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var crd = resource.CreateResourceDefinition();
            var result = await (string.IsNullOrWhiteSpace(resource.Metadata.NamespaceProperty)
                ? ApiClient.ReplaceClusterCustomObjectAsync(
                    resource,
                    crd.Group,
                    crd.Version,
                    crd.Plural,
                    resource.Metadata.Name)
                : ApiClient.ReplaceNamespacedCustomObjectAsync(
                    resource,
                    crd.Group,
                    crd.Version,
                    resource.Metadata.NamespaceProperty,
                    crd.Plural,
                    resource.Metadata.Name)) as JObject;

            if (result?.ToObject(resource.GetType()) is TResource parsed)
            {
                resource.Metadata.ResourceVersion = parsed.Metadata.ResourceVersion;
                return parsed;
            }

            throw new ArgumentException("Could not parse result");
        }

        public async Task UpdateStatus<TStatus>(IStatus<TStatus> resource)
        {
            if (!(resource is IKubernetesObject<V1ObjectMeta> kubernetesObject))
            {
                throw new ArgumentException("Resource is not a propper kubernetes object");
            }

            var crd = kubernetesObject.CreateResourceDefinition();
            var result = await (string.IsNullOrWhiteSpace(kubernetesObject.Metadata.NamespaceProperty)
                ? ApiClient.ReplaceClusterCustomObjectStatusAsync(
                    resource,
                    crd.Group,
                    crd.Version,
                    crd.Plural,
                    kubernetesObject.Metadata.Name)
                : ApiClient.ReplaceNamespacedCustomObjectStatusAsync(
                    resource,
                    crd.Group,
                    crd.Version,
                    kubernetesObject.Metadata.NamespaceProperty,
                    crd.Plural,
                    kubernetesObject.Metadata.Name)) as JObject;

            if (result?.ToObject(resource.GetType()) is IKubernetesObject<V1ObjectMeta> parsed)
            {
                kubernetesObject.Metadata.ResourceVersion = parsed.Metadata.ResourceVersion;
            }
        }

        public Task Delete<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta> => Delete<TResource>(
            resource.Metadata.Name,
            resource.Metadata.NamespaceProperty);

        public Task Delete<TResource>(IEnumerable<TResource> resources)
            where TResource : IKubernetesObject<V1ObjectMeta> =>
            Task.WhenAll(resources.Select(Delete));

        public Task Delete<TResource>(params TResource[] resources)
            where TResource : IKubernetesObject<V1ObjectMeta> =>
            Task.WhenAll(resources.Select(Delete));

        public async Task Delete<TResource>(string name, string? @namespace = null)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TResource>();
            await (string.IsNullOrWhiteSpace(@namespace)
                ? ApiClient.DeleteClusterCustomObjectAsync(
                    crd.Group,
                    crd.Version,
                    crd.Plural,
                    name)
                : ApiClient.DeleteNamespacedCustomObjectAsync(
                    crd.Group,
                    crd.Version,
                    @namespace,
                    crd.Plural,
                    name));
        }

        public Task<Watcher<TResource>> Watch<TResource>(
            TimeSpan timeout,
            Action<WatchEventType, TResource> onEvent,
            Action<Exception>? onError = null,
            Action? onClose = null,
            string? @namespace = null,
            CancellationToken cancellationToken = default)
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TResource>();
            var result = string.IsNullOrWhiteSpace(@namespace)
                ? ApiClient.ListClusterCustomObjectWithHttpMessagesAsync(
                    crd.Group,
                    crd.Version,
                    crd.Plural,
                    timeoutSeconds: (int) timeout.TotalSeconds,
                    watch: true,
                    cancellationToken: cancellationToken)
                : ApiClient.ListNamespacedCustomObjectWithHttpMessagesAsync(
                    crd.Group,
                    crd.Version,
                    @namespace,
                    crd.Plural,
                    timeoutSeconds: (int) timeout.TotalSeconds,
                    watch: true,
                    cancellationToken: cancellationToken);

            return Task.FromResult(
                result.Watch(
                    onEvent,
                    onError,
                    onClose));
        }
    }
}
