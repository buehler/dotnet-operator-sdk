using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.Client.LabelSelectors;

namespace KubeOps.Operator.Client
{
    public interface IKubernetesClient
    {
        IKubernetes ApiClient { get; }

        Task<TResource?> Get<TResource>(string name, string? @namespace = null)
            where TResource : class, IKubernetesObject<V1ObjectMeta>;

        Task<IList<TResource>> List<TResource>(
            string? @namespace = null,
            string? labelSelector = null)
            where TResource : IKubernetesObject<V1ObjectMeta>;

        Task<IList<TResource>> List<TResource>(
            string? @namespace = null,
            params ILabelSelector[] labelSelectors)
            where TResource : IKubernetesObject<V1ObjectMeta>;

        Task<TResource> Save<TResource>(TResource resource)
            where TResource : class, IKubernetesObject<V1ObjectMeta>;

        Task<TResource> Create<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>;

        Task<TResource> Update<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>;

        public Task UpdateStatus<TStatus>(IStatus<TStatus> resource);

        Task Delete<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>;

        Task Delete<TResource>(IEnumerable<TResource> resources)
            where TResource : IKubernetesObject<V1ObjectMeta>;

        Task Delete<TResource>(params TResource[] resources)
            where TResource : IKubernetesObject<V1ObjectMeta>;

        Task Delete<TResource>(string name, string? @namespace = null)
            where TResource : IKubernetesObject<V1ObjectMeta>;

        Task<Watcher<TResource>> Watch<TResource>(
            TimeSpan timeout,
            Action<WatchEventType, TResource> onEvent,
            Action<Exception>? onError = null,
            Action? onClose = null,
            string? @namespace = null,
            CancellationToken cancellationToken = default)
            where TResource : IKubernetesObject<V1ObjectMeta>;
    }
}
