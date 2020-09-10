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

        /// <summary>
        /// Returns the name of the current namespace.
        /// To determine the current namespace the following places (in the given order) are checked:
        /// <list type="number">
        /// <item>
        /// <description>The created kubernetes configuration (from file / incluster)</description>
        /// </item>
        /// <item>
        /// <description>
        ///     The env variable given as the param to the function (default "POD_NAMESPACE")
        ///     which can be provided by the <a href="https://kubernetes.io/docs/tasks/inject-data-application/downward-api-volume-expose-pod-information/#capabilities-of-the-downward-api">kubernetes downward API</a>
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        ///     The fallback secret file if running on the cluster
        ///     (/var/run/secrets/kubernetes.io/serviceaccount/namespace)
        /// </description>
        /// </item>
        /// <item>
        /// <description>"default"</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="downwardApiEnvName">Customizable name of the env var to check for the namespace.</param>
        /// <returns>A string containing the current namespace (or a fallback of it).</returns>
        Task<string> GetCurrentNamespace(string downwardApiEnvName = "POD_NAMESPACE");

        /// <summary>
        /// Fetch and return the actual kubernetes <see cref="VersionInfo"/> (aka. Server Version).
        /// </summary>
        /// <returns>The <see cref="VersionInfo"/> of the current server.</returns>
        Task<VersionInfo> GetServerVersion();

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
