using System.Net;
using k8s;
using k8s.Autorest;
using k8s.Models;
using KubeOps.KubernetesClient.Entities;
using KubeOps.KubernetesClient.LabelSelectors;

namespace KubeOps.KubernetesClient;

public class KubernetesClient : IKubernetesClient
{
    private const string DownwardApiNamespaceFile = "/var/run/secrets/kubernetes.io/serviceaccount/namespace";
    private const string DefaultNamespace = "default";

    private readonly KubernetesClientConfiguration _clientConfig;
    private readonly IKubernetes _client;

    public KubernetesClient()
        : this(KubernetesClientConfiguration.BuildDefaultConfig())
    {
    }

    public KubernetesClient(KubernetesClientConfiguration clientConfig)
        : this(clientConfig, new Kubernetes(clientConfig))
    {
    }

    public KubernetesClient(KubernetesClientConfiguration clientConfig, IKubernetes client)
    {
        _clientConfig = clientConfig;
        _client = client;
    }

    public Uri BaseUri => _client.BaseUri;

    /// <inheritdoc />
    public Task<string> GetCurrentNamespace(string downwardApiEnvName = "POD_NAMESPACE")
    {
        var result = DefaultNamespace;

        if (_clientConfig.Namespace != null)
        {
            result = _clientConfig.Namespace;
        }

        if (Environment.GetEnvironmentVariable(downwardApiEnvName) != null)
        {
            result = Environment.GetEnvironmentVariable(downwardApiEnvName) ?? string.Empty;
        }

        if (File.Exists(DownwardApiNamespaceFile))
        {
            var ns = File.ReadAllText(DownwardApiNamespaceFile);
            result = ns.Trim();
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<VersionInfo> GetServerVersion() => _client.Version.GetCodeAsync();

    /// <inheritdoc />
    public async Task<TResource?> Get<TResource>(
        string name,
        string? @namespace = null)
        where TResource : class, IKubernetesObject<V1ObjectMeta>
    {
        try
        {
            var client = CreateClient<TResource>();

            return await (string.IsNullOrWhiteSpace(@namespace)
                ? client.ReadAsync<TResource>(name)
                : client.ReadNamespacedAsync<TResource>(@namespace, name));
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IList<TResource>> List<TResource>(string? @namespace = null, string? labelSelector = null)
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        var definition = EntityDefinition.FromType<TResource>();
        var result = await (string.IsNullOrWhiteSpace(@namespace)
            ? _client.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync(
                definition.Group,
                definition.Version,
                definition.Plural,
                labelSelector: labelSelector)
            : _client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
                definition.Group,
                definition.Version,
                @namespace,
                definition.Plural,
                labelSelector: labelSelector));
        var list = KubernetesJson.Deserialize<EntityList<TResource>>(result.Body.ToString());
        return list.Items;
    }

    /// <inheritdoc />
    public Task<IList<TResource>> List<TResource>(
        string? @namespace = null,
        params ILabelSelector[] labelSelectors)
        where TResource : IKubernetesObject<V1ObjectMeta> =>
        List<TResource>(@namespace, labelSelectors.ToExpression());

    /// <inheritdoc />
    public Task<TResource> Save<TResource>(TResource resource)
        where TResource : class, IKubernetesObject<V1ObjectMeta> =>
        resource.Uid() is null
            ? Create(resource)
            : Update(resource);

    /// <inheritdoc />
    public async Task<TResource> Create<TResource>(TResource resource)
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        var client = CreateClient<TResource>();

        return await (string.IsNullOrWhiteSpace(resource.Namespace())
            ? client.CreateAsync(resource)
            : client.CreateNamespacedAsync(resource, resource.Namespace()));
    }

    /// <inheritdoc />
    public async Task<TResource> Update<TResource>(TResource resource)
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        var client = CreateClient<TResource>();

        return await (string.IsNullOrWhiteSpace(resource.Namespace())
            ? client.ReplaceAsync(resource, resource.Name())
            : client.ReplaceNamespacedAsync(resource, resource.Namespace(), resource.Name()));
    }

    /// <inheritdoc />
    public async Task UpdateStatus<TResource>(TResource resource)
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        var definition = EntityDefinition.FromType<TResource>();
        await (string.IsNullOrWhiteSpace(resource.Namespace())
            ? _client.CustomObjects.ReplaceClusterCustomObjectStatusAsync(
                resource,
                definition.Group,
                definition.Version,
                definition.Plural,
                resource.Name())
            : _client.CustomObjects.ReplaceNamespacedCustomObjectStatusAsync(
                resource,
                definition.Group,
                definition.Version,
                resource.Namespace(),
                definition.Plural,
                resource.Name()));
    }

    /// <inheritdoc />
    public Task Delete<TResource>(TResource resource)
        where TResource : IKubernetesObject<V1ObjectMeta> => Delete<TResource>(
        resource.Name(),
        resource.Namespace());

    /// <inheritdoc />
    public Task Delete<TResource>(IEnumerable<TResource> resources)
        where TResource : IKubernetesObject<V1ObjectMeta> =>
        Task.WhenAll(resources.Select(Delete));

    /// <inheritdoc />
    public Task Delete<TResource>(params TResource[] resources)
        where TResource : IKubernetesObject<V1ObjectMeta> =>
        Task.WhenAll(resources.Select(Delete));

    /// <inheritdoc />
    public async Task Delete<TResource>(string name, string? @namespace = null)
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        var client = CreateClient<TResource>();

        try
        {
            await (string.IsNullOrWhiteSpace(@namespace)
                ? client.DeleteAsync<TResource>(name)
                : client.DeleteNamespacedAsync<TResource>(@namespace, name));
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
        {
        }
    }

    /// <inheritdoc />
    public Task<Watcher<TResource>> Watch<TResource>(
        TimeSpan timeout,
        Action<WatchEventType, TResource> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        CancellationToken cancellationToken = default,
        params ILabelSelector[] labelSelectors)
        where TResource : IKubernetesObject<V1ObjectMeta>
        => Watch(
            timeout,
            onEvent,
            onError,
            onClose,
            @namespace,
            cancellationToken,
            string.Join(",", labelSelectors.Select(l => l.ToExpression())));

    /// <inheritdoc />
    public Task<Watcher<TResource>> Watch<TResource>(
        TimeSpan timeout,
        Action<WatchEventType, TResource> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        CancellationToken cancellationToken = default,
        string? labelSelector = null)
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        var crd = EntityDefinition.FromType<TResource>();
        var result = string.IsNullOrWhiteSpace(@namespace)
            ? _client.CustomObjects.ListClusterCustomObjectWithHttpMessagesAsync(
                crd.Group,
                crd.Version,
                crd.Plural,
                labelSelector: labelSelector,
                timeoutSeconds: (int)timeout.TotalSeconds,
                watch: true,
                cancellationToken: cancellationToken)
            : _client.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(
                crd.Group,
                crd.Version,
                @namespace,
                crd.Plural,
                labelSelector: labelSelector,
                timeoutSeconds: (int)timeout.TotalSeconds,
                watch: true,
                cancellationToken: cancellationToken);

        return Task.FromResult(
            result.Watch(
                onEvent,
                onError,
                onClose));
    }

    private GenericClient CreateClient<TResource>()
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        var definition = EntityDefinition.FromType<TResource>();
        return definition.Group switch
        {
            "" => new GenericClient(_client, definition.Version, definition.Plural),
            _ => new GenericClient(_client, definition.Group, definition.Version, definition.Plural),
        };
    }
}
