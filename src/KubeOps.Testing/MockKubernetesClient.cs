using System.Diagnostics.CodeAnalysis;
using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.KubernetesClient.LabelSelectors;

namespace KubeOps.Testing;

/// <summary>
/// Mocked implementation for the kubernetes client.
/// Returns the "result" objects if given.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1625:Element documentation should not be copied and pasted",
    Justification = "This is the mock implementation which basically does nothing.")]
public class MockKubernetesClient : IKubernetesClient
{
    /// <summary>
    /// Mocked result for the <see cref="Get{TEntity}"/> call.
    /// If null, then null is returned.
    /// This field must be manually reset.
    /// </summary>
    public object? GetResult { get; set; }

    /// <summary>
    /// Mocked result for the <see cref="List{TEntity}(string?,string?)"/> call.
    /// If null, an empty list is returned.
    /// This field must be manually reset.
    /// </summary>
    public IList<object>? ListResult { get; set; }

    /// <summary>
    /// Mocked result for the <see cref="Save{TEntity}"/> call.
    /// If null, then null is returned.
    /// This field must be manually reset.
    /// </summary>
    public object? SaveResult { get; set; }

    /// <summary>
    /// Mocked result for the <see cref="Create{TEntity}"/> call.
    /// If null, then null is returned.
    /// This field must be manually reset.
    /// </summary>
    public object? CreateResult { get; set; }

    /// <summary>
    /// Mocked result for the <see cref="Update{TEntity}"/> call.
    /// If null, then null is returned.
    /// This field must be manually reset.
    /// </summary>
    public object? UpdateResult { get; set; }

    public Uri BaseUri => new("https://localhost:8080");

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
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>The value of <see cref="GetResult"/>.</returns>
    public Task<TEntity?> Get<TEntity>(string name, string? @namespace = null)
        where TEntity : class, IKubernetesObject<V1ObjectMeta>
        => Task.FromResult(GetResult as TEntity);

    /// <summary>
    /// Mocked list method.
    /// </summary>
    /// <param name="namespace">Not used.</param>
    /// <param name="labelSelector">Not used.</param>
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>The value of <see cref="ListResult"/> or empty list if the result is null.</returns>
    public Task<IList<TEntity>> List<TEntity>(string? @namespace = null, string? labelSelector = null)
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => Task.FromResult(ListResult as IList<TEntity> ?? new List<TEntity>());

    /// <summary>
    /// Mocked list method.
    /// </summary>
    /// <param name="namespace">Not used.</param>
    /// <param name="labelSelectors">Not used.</param>
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>The value of <see cref="ListResult"/> or empty list if the result is null.</returns>
    public Task<IList<TEntity>> List<TEntity>(string? @namespace = null, params ILabelSelector[] labelSelectors)
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => Task.FromResult(ListResult as IList<TEntity> ?? new List<TEntity>());

    /// <summary>
    /// Mocked Save method.
    /// </summary>
    /// <param name="resource">Not used.</param>
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>The value of <see cref="SaveResult"/>.</returns>
    public Task<TEntity> Save<TEntity>(TEntity resource)
        where TEntity : class, IKubernetesObject<V1ObjectMeta>
        => Task.FromResult(SaveResult as TEntity)!;

    /// <summary>
    /// Mocked Create method.
    /// </summary>
    /// <param name="resource">Not used.</param>
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>The value of <see cref="CreateResult"/>.</returns>
    public Task<TEntity> Create<TEntity>(TEntity resource)
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => Task.FromResult((TEntity)CreateResult!)!;

    /// <summary>
    /// Mocked Update method.
    /// </summary>
    /// <param name="resource">Not used.</param>
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>The value of <see cref="UpdateResult"/>.</returns>
    public Task<TEntity> Update<TEntity>(TEntity resource)
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => Task.FromResult((TEntity)UpdateResult!)!;

    /// <summary>
    /// Mocked UpdateStatus method.
    /// </summary>
    /// <param name="resource">Not used.</param>
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>Empty completed task.</returns>
    public Task UpdateStatus<TEntity>(TEntity resource)
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => Task.CompletedTask;

    /// <summary>
    /// Mocked Delete method.
    /// </summary>
    /// <param name="resource">Not used.</param>
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>Empty completed task.</returns>
    public Task Delete<TEntity>(TEntity resource)
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => Task.CompletedTask;

    /// <summary>
    /// Mocked Delete method.
    /// </summary>
    /// <param name="resources">Not used.</param>
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>Empty completed task.</returns>
    public Task Delete<TEntity>(IEnumerable<TEntity> resources)
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => Task.CompletedTask;

    /// <summary>
    /// Mocked Delete method.
    /// </summary>
    /// <param name="resources">Not used.</param>
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>Empty completed task.</returns>
    public Task Delete<TEntity>(params TEntity[] resources)
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => Task.CompletedTask;

    /// <summary>
    /// Mocked Delete method.
    /// </summary>
    /// <param name="name">Not used.</param>
    /// <param name="namespace">Not used.</param>
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>Empty completed task.</returns>
    public Task Delete<TEntity>(string name, string? @namespace = null)
        where TEntity : IKubernetesObject<V1ObjectMeta>
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
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>Empty new watcher from a memory stream.</returns>
    public Task<Watcher<TEntity>> Watch<TEntity>(
        TimeSpan timeout,
        Action<WatchEventType, TEntity> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        CancellationToken cancellationToken = default,
        params ILabelSelector[] selectors)
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => Task.FromResult(
            new Watcher<TEntity>(
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
    /// <typeparam name="TEntity">Type of the resource.</typeparam>
    /// <returns>Empty new watcher from a memory stream.</returns>
    public Task<Watcher<TEntity>> Watch<TEntity>(
        TimeSpan timeout,
        Action<WatchEventType, TEntity> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        CancellationToken cancellationToken = default,
        string? labelSelector = default)
        where TEntity : IKubernetesObject<V1ObjectMeta>
        => Task.FromResult(
            new Watcher<TEntity>(
                () => Task.FromResult(new StreamReader(new MemoryStream())),
                (_, __) => { },
                _ => { }));
}
