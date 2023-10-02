using System.Net;

using k8s;
using k8s.Autorest;
using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.KubernetesClient.LabelSelectors;

namespace KubeOps.KubernetesClient;

/// <inheritdoc cref="IKubernetesClient{TEntity}"/>
public class KubernetesClient<TEntity> : IKubernetesClient<TEntity>, IDisposable
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
    public async Task<string> GetCurrentNamespace(string downwardApiEnvName = "POD_NAMESPACE")
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
    public async Task<TEntity?> Get(string name, string? @namespace = null)
    {
        var list = @namespace switch
        {
            null => await _client.CustomObjects.ListClusterCustomObjectAsync<EntityList<TEntity>>(
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                _metadata.PluralName),
            _ => await _client.CustomObjects.ListNamespacedCustomObjectAsync<EntityList<TEntity>>(
                _metadata.Group ?? string.Empty,
                _metadata.Version,
                @namespace,
                _metadata.PluralName),
        };

        return list switch
        {
            { Items: [var existing] } => existing,
            _ => default,
        };
    }

    /// <inheritdoc />
    public async Task<IList<TEntity>> List(string? @namespace = null, string? labelSelector = null)
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
    public Task<IList<TEntity>> List(string? @namespace = null, params LabelSelector[] labelSelectors)
        => List(@namespace, labelSelectors.ToExpression());

    /// <inheritdoc />
    public Task<TEntity> Create(TEntity entity)
        => entity.Namespace() switch
        {
            { } ns => _genericClient.CreateNamespacedAsync(entity, ns),
            null => _genericClient.CreateAsync(entity),
        };

    /// <inheritdoc />
    public Task<TEntity> Update(TEntity entity)
        => entity.Namespace() switch
        {
            { } ns => _genericClient.ReplaceNamespacedAsync(entity, ns, entity.Name()),
            null => _genericClient.ReplaceAsync(entity, entity.Name()),
        };

    /// <inheritdoc />
    public Task<TEntity> UpdateStatus(TEntity entity)
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
    public Task Delete(TEntity entity) => Delete(
        entity.Name(),
        entity.Namespace());

    /// <inheritdoc />
    public Task Delete(IEnumerable<TEntity> entities) =>
        Task.WhenAll(entities.Select(Delete));

    /// <inheritdoc />
    public Task Delete(params TEntity[] entities) =>
        Task.WhenAll(entities.Select(Delete));

    /// <inheritdoc />
    public async Task Delete(string name, string? @namespace = null)
    {
        try
        {
            switch (@namespace)
            {
                case not null:
                    await _genericClient.DeleteNamespacedAsync<TEntity>(@namespace, name);
                    break;
                default:
                    await _genericClient.DeleteAsync<TEntity>(name);
                    break;
            }
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
        {
            // The resource was not found. We can ignore this.
        }
    }

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
