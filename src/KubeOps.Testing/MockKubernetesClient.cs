using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using DotnetKubernetesClient.LabelSelectors;
using k8s;
using k8s.Models;

namespace KubeOps.Testing
{
    /// <summary>
    /// Mocked implementation for the kubernetes client.
    /// Returns the "result" objects if given.
    /// </summary>
    public class MockKubernetesClient : IKubernetesClient
    {
        /// <summary>
        /// Instance of the default client.
        /// </summary>
        public IKubernetes ApiClient { get; } = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());

        /// <summary>
        /// Mocked result for the <see cref="Get{TResource}"/> call.
        /// If null, then null is returned.
        /// This field must be manually reset.
        /// </summary>
        public object? GetResult { get; set; }

        /// <summary>
        /// Mocked result for the <see cref="List{TResource}(string?,string?)"/> call.
        /// If null, an empty list is returned.
        /// This field must be manually reset.
        /// </summary>
        public IList<object>? ListResult { get; set; }

        /// <summary>
        /// Mocked result for the <see cref="Save{TResource}"/> call.
        /// If null, then null is returned.
        /// This field must be manually reset.
        /// </summary>
        public object? SaveResult { get; set; }

        /// <summary>
        /// Mocked result for the <see cref="Create{TResource}"/> call.
        /// If null, then null is returned.
        /// This field must be manually reset.
        /// </summary>
        public object? CreateResult { get; set; }

        /// <summary>
        /// Mocked result for the <see cref="Update{TResource}"/> call.
        /// If null, then null is returned.
        /// This field must be manually reset.
        /// </summary>
        public object? UpdateResult { get; set; }

        /// <summary>
        /// Returns "default".
        /// </summary>
        /// <param name="downwardApiEnvName">Not used env name.</param>
        /// <returns>"default".</returns>
        public Task<string> GetCurrentNamespace(string downwardApiEnvName = "POD_NAMESPACE") =>
            Task.FromResult("default");

        /// <summary>
        /// Create mocked server version.
        /// </summary>
        /// <returns>Empty "new" <see cref="VersionInfo"/> instance.</returns>
        public Task<VersionInfo> GetServerVersion() => Task.FromResult(new VersionInfo());

        /// <summary>
        /// Mocked Get method.
        /// </summary>
        /// <param name="name">Not used.</param>
        /// <param name="namespace">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>The value of <see cref="GetResult"/>.</returns>
        public Task<TResource?> Get<TResource>(string name, string? @namespace = null)
            where TResource : class, IKubernetesObject<V1ObjectMeta>
            => Task.FromResult(GetResult as TResource);

        /// <summary>
        /// Mocked list method.
        /// </summary>
        /// <param name="namespace">Not used.</param>
        /// <param name="labelSelector">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>The value of <see cref="ListResult"/> or empty list if the result is null.</returns>
        public Task<IList<TResource>> List<TResource>(string? @namespace = null, string? labelSelector = null)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.FromResult(ListResult as IList<TResource> ?? new List<TResource>());

        /// <summary>
        /// Mocked list method.
        /// </summary>
        /// <param name="namespace">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>The value of <see cref="ListResult"/> or empty list if the result is null.</returns>
        public Task<IList<TResource>> List<TResource>(string? @namespace = null, params ILabelSelector[] labelSelectors)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.FromResult(ListResult as IList<TResource> ?? new List<TResource>());

        /// <summary>
        /// Mocked Save method.
        /// </summary>
        /// <param name="resource">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>The value of <see cref="SaveResult"/>.</returns>
        public Task<TResource> Save<TResource>(TResource resource)
            where TResource : class, IKubernetesObject<V1ObjectMeta>
            => Task.FromResult(SaveResult as TResource)!;

        /// <summary>
        /// Mocked Create method.
        /// </summary>
        /// <param name="resource">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>The value of <see cref="CreateResult"/>.</returns>
        public Task<TResource> Create<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.FromResult((TResource)CreateResult!)!;

        /// <summary>
        /// Mocked Update method.
        /// </summary>
        /// <param name="resource">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>The value of <see cref="UpdateResult"/>.</returns>
        public Task<TResource> Update<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.FromResult((TResource)UpdateResult!)!;

        /// <summary>
        /// Mocked UpdateStatus method.
        /// </summary>
        /// <param name="resource">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>Empty completed task.</returns>
        public Task UpdateStatus<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>, IStatus<object>
            => Task.CompletedTask;

        /// <summary>
        /// Mocked Delete method.
        /// </summary>
        /// <param name="resource">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>Empty completed task.</returns>
        public Task Delete<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.CompletedTask;

        /// <summary>
        /// Mocked Delete method.
        /// </summary>
        /// <param name="resources">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>Empty completed task.</returns>
        public Task Delete<TResource>(IEnumerable<TResource> resources)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.CompletedTask;

        /// <summary>
        /// Mocked Delete method.
        /// </summary>
        /// <param name="resources">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>Empty completed task.</returns>
        public Task Delete<TResource>(params TResource[] resources)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.CompletedTask;

        /// <summary>
        /// Mocked Delete method.
        /// </summary>
        /// <param name="name">Not used.</param>
        /// <param name="namespace">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>Empty completed task.</returns>
        public Task Delete<TResource>(string name, string? @namespace = null)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.CompletedTask;

        /// <summary>
        /// Mocked Watch method.
        /// </summary>
        /// <param name="timeout">Not used.</param>
        /// <param name="onEvent">Not used.</param>
        /// <param name="onError">Not used.</param>
        /// <param name="onClose">Not used.</param>
        /// <param name="namespace">Not used.</param>
        /// <param name="cancellationToken">Not used.</param>
        /// <param name="selectors">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>Empty new watcher from a memory stream.</returns>
        public Task<Watcher<TResource>> Watch<TResource>(
            TimeSpan timeout,
            Action<WatchEventType, TResource> onEvent,
            Action<Exception>? onError = null,
            Action? onClose = null,
            string? @namespace = null,
            CancellationToken cancellationToken = default,
            params ILabelSelector[] selectors)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.FromResult(
                new Watcher<TResource>(
                    () => Task.FromResult(new StreamReader(new MemoryStream())),
                    (_, __) => { },
                    _ => { }));

        /// <summary>
        /// Mocked Watch method.
        /// </summary>
        /// <param name="timeout">Not used.</param>
        /// <param name="onEvent">Not used.</param>
        /// <param name="onError">Not used.</param>
        /// <param name="onClose">Not used.</param>
        /// <param name="namespace">Not used.</param>
        /// <param name="cancellationToken">Not used.</param>
        /// <param name="labelSelector">Not used.</param>
        /// <typeparam name="TResource">Type of the resource.</typeparam>
        /// <returns>Empty new watcher from a memory stream.</returns>
        public Task<Watcher<TResource>> Watch<TResource>(
            TimeSpan timeout,
            Action<WatchEventType, TResource> onEvent,
            Action<Exception>? onError = null,
            Action? onClose = null,
            string? @namespace = null,
            CancellationToken cancellationToken = default,
            string? labelSelector = default)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.FromResult(
                new Watcher<TResource>(
                    () => Task.FromResult(new StreamReader(new MemoryStream())),
                    (_, __) => { },
                    _ => { }));
    }
}
