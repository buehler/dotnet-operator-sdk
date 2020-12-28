﻿using System;
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
    public class MockKubernetesClient : IKubernetesClient
    {
        public IKubernetes ApiClient { get; } = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());

        public object? GetResult { get; set; }

        public IList<object>? ListResult { get; set; }

        public object? SaveResult { get; set; }

        public object? CreateResult { get; set; }

        public object? UpdateResult { get; set; }

        public Task<string> GetCurrentNamespace(string downwardApiEnvName = "POD_NAMESPACE") =>
            Task.FromResult("default");

        public Task<VersionInfo> GetServerVersion() => Task.FromResult(new VersionInfo());

        public Task<TResource?> Get<TResource>(string name, string? @namespace = null)
            where TResource : class, IKubernetesObject<V1ObjectMeta>
            => Task.FromResult(GetResult as TResource);

        public Task<IList<TResource>> List<TResource>(string? @namespace = null, string? labelSelector = null)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.FromResult(ListResult as IList<TResource> ?? new List<TResource>());

        public Task<IList<TResource>> List<TResource>(string? @namespace = null, params ILabelSelector[] labelSelectors)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.FromResult(ListResult as IList<TResource> ?? new List<TResource>());

        public Task<TResource> Save<TResource>(TResource resource)
            where TResource : class, IKubernetesObject<V1ObjectMeta>
            => Task.FromResult(SaveResult as TResource)!;

        public Task<TResource> Create<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.FromResult((TResource)CreateResult!)!;

        public Task<TResource> Update<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.FromResult((TResource)UpdateResult!)!;

        public Task UpdateStatus<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>, IStatus<object>
            => Task.CompletedTask;

        public Task Delete<TResource>(TResource resource)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.CompletedTask;

        public Task Delete<TResource>(IEnumerable<TResource> resources)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.CompletedTask;

        public Task Delete<TResource>(params TResource[] resources)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.CompletedTask;

        public Task Delete<TResource>(string name, string? @namespace = null)
            where TResource : IKubernetesObject<V1ObjectMeta>
            => Task.CompletedTask;

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
