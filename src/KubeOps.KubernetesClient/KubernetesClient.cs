﻿using System.Collections.Concurrent;
using System.Net;

using k8s;
using k8s.Autorest;
using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Transpiler;

namespace KubeOps.KubernetesClient;

/// <inheritdoc cref="IKubernetesClient"/>
public class KubernetesClient : IKubernetesClient
{
    private const string DownwardApiNamespaceFile = "/var/run/secrets/kubernetes.io/serviceaccount/namespace";
    private const string DefaultNamespace = "default";

    private static readonly ConcurrentDictionary<Type, EntityMetadata> MetadataCache = new();

    private readonly KubernetesClientConfiguration _clientConfig;
    private readonly IKubernetes _client;

    /// <summary>
    /// Create a new Kubernetes client for the given entity.
    /// The client will use the default configuration.
    /// </summary>
    public KubernetesClient()
        : this(KubernetesClientConfiguration.BuildDefaultConfig())
    {
    }

    /// <summary>
    /// Create a new Kubernetes client for the given entity with a custom client configuration.
    /// </summary>
    /// <param name="clientConfig">The config for the underlying Kubernetes client.</param>
    public KubernetesClient(KubernetesClientConfiguration clientConfig)
        : this(clientConfig, new Kubernetes(clientConfig))
    {
    }

    /// <summary>
    /// Create a new Kubernetes client for the given entity with a custom client configuration and client.
    /// </summary>
    /// <param name="clientConfig">The config for the underlying Kubernetes client.</param>
    /// <param name="client">The underlying client.</param>
    public KubernetesClient(KubernetesClientConfiguration clientConfig, IKubernetes client)
    {
        _clientConfig = clientConfig;
        _client = client;
    }

    /// <inheritdoc />
    public Uri BaseUri => _client.BaseUri;

    public static void ClearMetadataCache() => MetadataCache.Clear();

    /// <inheritdoc />
    public async Task<string> GetCurrentNamespaceAsync(string downwardApiEnvName = "POD_NAMESPACE")
    {
        if (_clientConfig.Namespace is { } configValue)
        {
            return configValue;
        }

        if (Environment.GetEnvironmentVariable(downwardApiEnvName) is { } envValue)
        {
            return envValue;
        }

        if (File.Exists(DownwardApiNamespaceFile))
        {
            var ns = await File.ReadAllTextAsync(DownwardApiNamespaceFile);
            return ns.Trim();
        }

        return DefaultNamespace;
    }

    /// <inheritdoc />
    public string GetCurrentNamespace(string downwardApiEnvName = "POD_NAMESPACE")
    {
        if (_clientConfig.Namespace is { } configValue)
        {
            return configValue;
        }

        if (Environment.GetEnvironmentVariable(downwardApiEnvName) is { } envValue)
        {
            return envValue;
        }

        if (File.Exists(DownwardApiNamespaceFile))
        {
            var ns = File.ReadAllText(DownwardApiNamespaceFile);
            return ns.Trim();
        }

        return DefaultNamespace;
    }

    /// <inheritdoc />
    public async Task<TEntity?> GetAsync<TEntity>(string name, string? @namespace = null)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        var metadata = GetMetadata<TEntity>();
        var list = @namespace switch
        {
            null => await _client.CustomObjects.ListClusterCustomObjectAsync<EntityList<TEntity>>(
                metadata.Group ?? string.Empty,
                metadata.Version,
                metadata.PluralName,
                fieldSelector: $"metadata.name={name}"),
            _ => await _client.CustomObjects.ListNamespacedCustomObjectAsync<EntityList<TEntity>>(
                metadata.Group ?? string.Empty,
                metadata.Version,
                @namespace,
                metadata.PluralName,
                fieldSelector: $"metadata.name={name}"),
        };

        return list switch
        {
            { Items: [var existing] } => existing,
            _ => default,
        };
    }

    /// <inheritdoc />
    public TEntity? Get<TEntity>(string name, string? @namespace = null)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        var metadata = GetMetadata<TEntity>();
        var list = @namespace switch
        {
            null => _client.CustomObjects.ListClusterCustomObject<EntityList<TEntity>>(
                metadata.Group ?? string.Empty,
                metadata.Version,
                metadata.PluralName,
                fieldSelector: $"metadata.name={name}"),
            _ => _client.CustomObjects.ListNamespacedCustomObject<EntityList<TEntity>>(
                metadata.Group ?? string.Empty,
                metadata.Version,
                @namespace,
                metadata.PluralName,
                fieldSelector: $"metadata.name={name}"),
        };

        return list switch
        {
            { Items: [var existing] } => existing,
            _ => default,
        };
    }

    /// <inheritdoc />
    public async Task<IList<TEntity>> ListAsync<TEntity>(string? @namespace = null, string? labelSelector = null)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        var metadata = GetMetadata<TEntity>();
        return (@namespace switch
        {
            null => await _client.CustomObjects.ListClusterCustomObjectAsync<EntityList<TEntity>>(
                metadata.Group ?? string.Empty,
                metadata.Version,
                metadata.PluralName,
                labelSelector: labelSelector),
            _ => await _client.CustomObjects.ListNamespacedCustomObjectAsync<EntityList<TEntity>>(
                metadata.Group ?? string.Empty,
                metadata.Version,
                @namespace,
                metadata.PluralName,
                labelSelector: labelSelector),
        }).Items;
    }

    /// <inheritdoc />
    public IList<TEntity> List<TEntity>(string? @namespace = null, string? labelSelector = null)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        var metadata = GetMetadata<TEntity>();
        return (@namespace switch
        {
            null => _client.CustomObjects.ListClusterCustomObject<EntityList<TEntity>>(
                metadata.Group ?? string.Empty,
                metadata.Version,
                metadata.PluralName,
                labelSelector: labelSelector),
            _ => _client.CustomObjects.ListNamespacedCustomObject<EntityList<TEntity>>(
                metadata.Group ?? string.Empty,
                metadata.Version,
                @namespace,
                metadata.PluralName,
                labelSelector: labelSelector),
        }).Items;
    }

    /// <inheritdoc />
    public async Task<TEntity> CreateAsync<TEntity>(TEntity entity)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        using var client = CreateGenericClient<TEntity>();
        return await (entity.Namespace() switch
        {
            { } ns => client.CreateNamespacedAsync(entity, ns),
            null => client.CreateAsync(entity),
        });
    }

    /// <inheritdoc />
    public async Task<TEntity> UpdateAsync<TEntity>(TEntity entity)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        using var client = CreateGenericClient<TEntity>();
        return await (entity.Namespace() switch
        {
            { } ns => client.ReplaceNamespacedAsync(entity, ns, entity.Name()),
            null => client.ReplaceAsync(entity, entity.Name()),
        });
    }

    /// <inheritdoc />
    public Task<TEntity> UpdateStatusAsync<TEntity>(TEntity entity)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        var metadata = GetMetadata<TEntity>();
        return entity.Namespace() switch
        {
            { } ns => _client.CustomObjects.ReplaceNamespacedCustomObjectStatusAsync<TEntity>(
                entity,
                metadata.Group ?? string.Empty,
                metadata.Version,
                ns,
                metadata.PluralName,
                entity.Name()),
            _ => _client.CustomObjects.ReplaceClusterCustomObjectStatusAsync<TEntity>(
                entity,
                metadata.Group ?? string.Empty,
                metadata.Version,
                metadata.PluralName,
                entity.Name()),
        };
    }

    /// <inheritdoc />
    public TEntity UpdateStatus<TEntity>(TEntity entity)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        var metadata = GetMetadata<TEntity>();
        return entity.Namespace() switch
        {
            { } ns => _client.CustomObjects.ReplaceNamespacedCustomObjectStatus<TEntity>(
                entity,
                metadata.Group ?? string.Empty,
                metadata.Version,
                ns,
                metadata.PluralName,
                entity.Name()),
            _ => _client.CustomObjects.ReplaceClusterCustomObjectStatus<TEntity>(
                entity,
                metadata.Group ?? string.Empty,
                metadata.Version,
                metadata.PluralName,
                entity.Name()),
        };
    }

    /// <inheritdoc />
    public async Task DeleteAsync<TEntity>(string name, string? @namespace = null)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        try
        {
            using var client = CreateGenericClient<TEntity>();
            switch (@namespace)
            {
                case not null:
                    await client.DeleteNamespacedAsync<V1Status>(@namespace, name);
                    break;
                default:
                    await client.DeleteAsync<V1Status>(name);
                    break;
            }
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
        {
            // The resource was not found. We can ignore this.
        }
    }

    /// <inheritdoc />
    public Watcher<TEntity> Watch<TEntity>(
        Action<WatchEventType, TEntity> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        TimeSpan? timeout = null,
        string? labelSelector = null,
        CancellationToken cancellationToken = default)
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        var metadata = GetMetadata<TEntity>();
        return (@namespace switch
        {
            not null => _client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
                metadata.Group ?? string.Empty,
                metadata.Version,
                @namespace,
                metadata.PluralName,
                labelSelector: labelSelector,
                timeoutSeconds: timeout switch
                {
                    null => null,
                    _ => (int?)timeout.Value.TotalSeconds,
                },
                watch: true,
                cancellationToken: cancellationToken),
            _ => _client.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync(
                metadata.Group ?? string.Empty,
                metadata.Version,
                metadata.PluralName,
                labelSelector: labelSelector,
                timeoutSeconds: timeout switch
                {
                    null => null,
                    _ => (int?)timeout.Value.TotalSeconds,
                },
                watch: true,
                cancellationToken: cancellationToken),
        }).Watch(onEvent, onError, onClose);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _client.Dispose();
    }

    private static EntityMetadata GetMetadata<TEntity>()
    {
        var type = typeof(TEntity);
        return MetadataCache.GetOrAdd(type, t => Entities.ToEntityMetadata(t).Metadata);
    }

    private GenericClient CreateGenericClient<TEntity>()
    {
        var metadata = GetMetadata<TEntity>();
        return metadata.Group switch
        {
            null => new GenericClient(
                _client,
                metadata.Version,
                metadata.PluralName,
                false),
            _ => new GenericClient(
                _client,
                metadata.Group,
                metadata.Version,
                metadata.PluralName,
                false),
        };
    }
}
