﻿using System.Net;

using k8s;
using k8s.Autorest;
using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.KubernetesClient.LabelSelectors;

namespace KubeOps.KubernetesClient;

/// <inheritdoc cref="IKubernetesClient{TEntity}"/>
public class KubernetesClient<TEntity> : IKubernetesClient<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private const string DownwardApiNamespaceFile = "/var/run/secrets/kubernetes.io/serviceaccount/namespace";
    private const string DefaultNamespace = "default";

    private readonly EntityMetadata _metadata;
    private readonly KubernetesClientConfiguration _clientConfig;
    private readonly IKubernetes _client;
    private readonly GenericClient _genericClient;

    /// <summary>
    /// Create a new Kubernetes client for the given entity.
    /// The client will use the default configuration.
    /// </summary>
    /// <param name="metadata">The metadata of the entity.</param>
    public KubernetesClient(EntityMetadata metadata)
        : this(metadata, KubernetesClientConfiguration.BuildDefaultConfig())
    {
    }

    /// <summary>
    /// Create a new Kubernetes client for the given entity with a custom client configuration.
    /// </summary>
    /// <param name="metadata">The metadata of the entity.</param>
    /// <param name="clientConfig">The config for the underlying Kubernetes client.</param>
    public KubernetesClient(EntityMetadata metadata, KubernetesClientConfiguration clientConfig)
        : this(metadata, clientConfig, new Kubernetes(clientConfig))
    {
    }

    /// <summary>
    /// Create a new Kubernetes client for the given entity with a custom client configuration and client.
    /// </summary>
    /// <param name="metadata">The metadata of the entity.</param>
    /// <param name="clientConfig">The config for the underlying Kubernetes client.</param>
    /// <param name="client">The underlying client.</param>
    public KubernetesClient(EntityMetadata metadata, KubernetesClientConfiguration clientConfig, IKubernetes client)
    {
        _metadata = metadata;
        _clientConfig = clientConfig;
        _client = client;
        _genericClient = metadata.Group switch
        {
            null => new GenericClient(
                client,
                metadata.Version,
                metadata.PluralName,
                false),
            _ => new GenericClient(
                client,
                metadata.Group,
                metadata.Version,
                metadata.PluralName,
                false),
        };
    }

    /// <inheritdoc />
    public Uri BaseUri => _client.BaseUri;

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
    public async Task<TEntity?> GetAsync(string name, string? @namespace = null)
    {
        var list = @namespace switch
        {
            null => await _client.CustomObjects.ListClusterCustomObjectAsync<EntityList<TEntity>>(
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                _metadata.PluralName,
                fieldSelector: $"metadata.name={name}"),
            _ => await _client.CustomObjects.ListNamespacedCustomObjectAsync<EntityList<TEntity>>(
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                @namespace,
                _metadata.PluralName,
                fieldSelector: $"metadata.name={name}"),
        };

        return list switch
        {
            { Items: [var existing] } => existing,
            _ => default,
        };
    }

    /// <inheritdoc />
    public TEntity? Get(string name, string? @namespace = null)
    {
        var list = @namespace switch
        {
            null => _client.CustomObjects.ListClusterCustomObject<EntityList<TEntity>>(
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                _metadata.PluralName,
                fieldSelector: $"metadata.name={name}"),
            _ => _client.CustomObjects.ListNamespacedCustomObject<EntityList<TEntity>>(
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                @namespace,
                _metadata.PluralName,
                fieldSelector: $"metadata.name={name}"),
        };

        return list switch
        {
            { Items: [var existing] } => existing,
            _ => default,
        };
    }

    /// <inheritdoc />
    public async Task<IList<TEntity>> ListAsync(string? @namespace = null, string? labelSelector = null)
        => (@namespace switch
        {
            null => await _client.CustomObjects.ListClusterCustomObjectAsync<EntityList<TEntity>>(
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                _metadata.PluralName,
                labelSelector: labelSelector),
            _ => await _client.CustomObjects.ListNamespacedCustomObjectAsync<EntityList<TEntity>>(
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                @namespace,
                _metadata.PluralName,
                labelSelector: labelSelector),
        }).Items;

    /// <inheritdoc />
    public Task<IList<TEntity>> ListAsync(string? @namespace = null, params LabelSelector[] labelSelectors)
        => ListAsync(@namespace, labelSelectors.ToExpression());

    /// <inheritdoc />
    public IList<TEntity> List(string? @namespace = null, string? labelSelector = null)
        => (@namespace switch
        {
            null => _client.CustomObjects.ListClusterCustomObject<EntityList<TEntity>>(
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                _metadata.PluralName,
                labelSelector: labelSelector),
            _ => _client.CustomObjects.ListNamespacedCustomObject<EntityList<TEntity>>(
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                @namespace,
                _metadata.PluralName,
                labelSelector: labelSelector),
        }).Items;

    /// <inheritdoc />
    public IList<TEntity> List(string? @namespace = null, params LabelSelector[] labelSelectors)
        => List(@namespace, labelSelectors.ToExpression());

    /// <inheritdoc />
    public Task<TEntity> CreateAsync(TEntity entity)
        => entity.Namespace() switch
        {
            { } ns => _genericClient.CreateNamespacedAsync(entity, ns),
            null => _genericClient.CreateAsync(entity),
        };

    /// <inheritdoc />
    public TEntity Create(TEntity entity)
        => CreateAsync(entity).GetAwaiter().GetResult();

    /// <inheritdoc />
    public Task<TEntity> UpdateAsync(TEntity entity)
        => entity.Namespace() switch
        {
            { } ns => _genericClient.ReplaceNamespacedAsync(entity, ns, entity.Name()),
            null => _genericClient.ReplaceAsync(entity, entity.Name()),
        };

    /// <inheritdoc />
    public TEntity Update(TEntity entity)
        => UpdateAsync(entity).GetAwaiter().GetResult();

    /// <inheritdoc />
    public Task<TEntity> UpdateStatusAsync(TEntity entity)
        => entity.Namespace() switch
        {
            { } ns => _client.CustomObjects.ReplaceNamespacedCustomObjectStatusAsync<TEntity>(
                entity,
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                ns,
                _metadata.PluralName,
                entity.Name()),
            _ => _client.CustomObjects.ReplaceClusterCustomObjectStatusAsync<TEntity>(
                entity,
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                _metadata.PluralName,
                entity.Name()),
        };

    /// <inheritdoc />
    public TEntity UpdateStatus(TEntity entity)
        => entity.Namespace() switch
        {
            { } ns => _client.CustomObjects.ReplaceNamespacedCustomObjectStatus<TEntity>(
                entity,
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                ns,
                _metadata.PluralName,
                entity.Name()),
            _ => _client.CustomObjects.ReplaceClusterCustomObjectStatus<TEntity>(
                entity,
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                _metadata.PluralName,
                entity.Name()),
        };

    /// <inheritdoc />
    public Task DeleteAsync(TEntity entity) => DeleteAsync(
        entity.Name(),
        entity.Namespace());

    /// <inheritdoc />
    public Task DeleteAsync(IEnumerable<TEntity> entities) =>
        Task.WhenAll(entities.Select(DeleteAsync));

    /// <inheritdoc />
    public Task DeleteAsync(params TEntity[] entities) =>
        Task.WhenAll(entities.Select(DeleteAsync));

    /// <inheritdoc />
    public async Task DeleteAsync(string name, string? @namespace = null)
    {
        try
        {
            switch (@namespace)
            {
                case not null:
                    await _genericClient.DeleteNamespacedAsync<V1Status>(@namespace, name);
                    break;
                default:
                    await _genericClient.DeleteAsync<V1Status>(name);
                    break;
            }
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
        {
            // The resource was not found. We can ignore this.
        }
    }

    /// <inheritdoc />
    public void Delete(TEntity entity)
        => DeleteAsync(entity).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void Delete(IEnumerable<TEntity> entities)
        => DeleteAsync(entities).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void Delete(params TEntity[] entities)
        => DeleteAsync(entities).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void Delete(string name, string? @namespace = null)
        => DeleteAsync(name, @namespace).GetAwaiter().GetResult();

    /// <inheritdoc />
    public Watcher<TEntity> Watch(
        Action<WatchEventType, TEntity> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default,
        params LabelSelector[] labelSelectors)
        => Watch(
            onEvent,
            onError,
            onClose,
            @namespace,
            timeout,
            labelSelectors.ToExpression(),
            cancellationToken);

    /// <inheritdoc />
    public Watcher<TEntity> Watch(
        Action<WatchEventType, TEntity> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        TimeSpan? timeout = null,
        string? labelSelector = null,
        CancellationToken cancellationToken = default)
        => (@namespace switch
        {
            not null => _client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                @namespace,
                _metadata.PluralName,
                labelSelector: labelSelector,
                timeoutSeconds: timeout switch
                {
                    null => null,
                    _ => (int?)timeout.Value.TotalSeconds,
                },
                watch: true,
                cancellationToken: cancellationToken),
            _ => _client.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync(
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                _metadata.PluralName,
                labelSelector: labelSelector,
                timeoutSeconds: timeout switch
                {
                    null => null,
                    _ => (int?)timeout.Value.TotalSeconds,
                },
                watch: true,
                cancellationToken: cancellationToken),
        }).Watch(onEvent, onError, onClose);

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
        _genericClient.Dispose();
    }
}
