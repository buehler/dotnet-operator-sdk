using k8s;
using k8s.Models;

using KubeOps.KubernetesClient.LabelSelectors;

namespace KubeOps.KubernetesClient;

/// <summary>
/// Client for the Kubernetes API. Contains various methods to manage Kubernetes entities.
/// </summary>
public interface IKubernetesClient
{
    /// <summary>
    /// Represents the "original" kubernetes client from the
    /// "KubernetesClient" package.
    /// </summary>
    IKubernetes ApiClient { get; }

    /// <summary>
    /// Return the base URI of the currently used KubernetesClient.
    /// </summary>
    Uri BaseUri { get; }

    /// <summary>
    /// Returns the name of the current namespace.
    /// To determine the current namespace the following places (in the given order) are checked:
    /// <list type="number">
    /// <item>
    /// <description>The created Kubernetes configuration (from file / in-cluster)</description>
    /// </item>
    /// <item>
    /// <description>
    ///     The env variable given as the param to the function (default "POD_NAMESPACE")
    ///     which can be provided by the <a href="https://Kubernetes.io/docs/tasks/inject-data-application/downward-API-volume-expose-pod-information/#capabilities-of-the-downward-API">Kubernetes downward API</a>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    ///     The fallback secret file if running on the cluster
    ///     (/var/run/secrets/Kubernetes.io/serviceaccount/namespace)
    /// </description>
    /// </item>
    /// <item>
    /// <description>`default`</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="downwardApiEnvName">Customizable name of the env var to check for the namespace.</param>
    /// <returns>A string containing the current namespace (or a fallback of it).</returns>
    Task<string> GetCurrentNamespace(string downwardApiEnvName = "POD_NAMESPACE");

    /// <summary>
    /// Fetch and return the actual Kubernetes <see cref="VersionInfo"/> (aka. Server Version).
    /// </summary>
    /// <returns>The <see cref="VersionInfo"/> of the current server.</returns>
    Task<VersionInfo> GetServerVersion();

    /// <summary>
    /// Fetch and return a entity from the Kubernetes API.
    /// </summary>
    /// <param name="name">The name of the entity (metadata.name).</param>
    /// <param name="namespace">
    /// Optional namespace. If this is set, the entity must be a namespaced entity.
    /// If it is omitted, the entity must be a cluster wide entity.
    /// </param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>The found entity of the given type, or null otherwise.</returns>
    Task<TEntity?> Get<TEntity>(string name, string? @namespace = null)
        where TEntity : class, IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Fetch and return a list of entities from the Kubernetes API.
    /// </summary>
    /// <param name="namespace">If the entities are namespaced, provide the name of the namespace.</param>
    /// <param name="labelSelector">A string, representing an optional label selector for filtering fetched objects.</param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>A list of Kubernetes entities.</returns>
    Task<IList<TEntity>> List<TEntity>(
        string? @namespace = null,
        string? labelSelector = null)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Fetch and return a list of entities from the Kubernetes API.
    /// </summary>
    /// <param name="namespace">
    /// If only entities in a given namespace should be listed, provide the namespace here.
    /// </param>
    /// <param name="labelSelectors">A list of label-selectors to apply to the search.</param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>A list of Kubernetes entities.</returns>
    Task<IList<TEntity>> List<TEntity>(
        string? @namespace = null,
        params ILabelSelector[] labelSelectors)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Create or Update a entity. This first fetches the entity from the Kubernetes API
    /// and if it does exist, updates the entity. Otherwise, the entity is created.
    /// </summary>
    /// <param name="entity">The entity in question.</param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>The saved instance of the entity.</returns>
    Task<TEntity> Save<TEntity>(TEntity entity)
        where TEntity : class, IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Create the given entity on the Kubernetes API.
    /// </summary>
    /// <param name="entity">The entity instance.</param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>The created instance of the entity.</returns>
    Task<TEntity> Create<TEntity>(TEntity entity)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Update the given entity on the Kubernetes API.
    /// </summary>
    /// <param name="entity">The entity instance.</param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>The updated instance of the entity.</returns>
    Task<TEntity> Update<TEntity>(TEntity entity)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Update the status object of a given entity on the Kubernetes API.
    /// </summary>
    /// <param name="entity">The entity that contains a status object.</param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>A task that completes when the call was made.</returns>
    public Task UpdateStatus<TEntity>(TEntity entity)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Delete a given entity from the Kubernetes API.
    /// </summary>
    /// <param name="entity">The entity in question.</param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>A task that completes when the call was made.</returns>
    Task Delete<TEntity>(TEntity entity)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Delete a given list of entities from the Kubernetes API.
    /// </summary>
    /// <param name="entities">The entities in question.</param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>A task that completes when the calls were made.</returns>
    Task Delete<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Delete a given list of entities from the Kubernetes API.
    /// </summary>
    /// <param name="entities">The entities in question.</param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>A task that completes when the calls were made.</returns>
    Task Delete<TEntity>(params TEntity[] entities)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Delete a given entity by name from the Kubernetes API.
    /// </summary>
    /// <param name="name">The name of the entity.</param>
    /// <param name="namespace">The optional namespace of the entity.</param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>A task that completes when the call was made.</returns>
    Task Delete<TEntity>(string name, string? @namespace = null)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Create a entity watcher on the Kubernetes API.
    /// The entity watcher fires events for entity-events on
    /// Kubernetes (events: <see cref="WatchEventType"/>.
    /// </summary>
    /// <param name="timeout">The timeout which the watcher has (after this timeout, the server will close the connection).</param>
    /// <param name="onEvent">Action that is called when an event occurs.</param>
    /// <param name="onError">Action that handles exceptions.</param>
    /// <param name="onClose">Action that handles closed connections.</param>
    /// <param name="namespace">
    /// The namespace to watch for entities (if needed).
    /// If the namespace is omitted, all entities on the cluster are watched.
    /// </param>
    /// <param name="cancellationToken">Cancellation-Token.</param>
    /// <param name="labelSelectors">A list of label-selectors to apply to the search.</param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>A entity watcher for the given entity.</returns>
    Task<Watcher<TEntity>> Watch<TEntity>(
        TimeSpan timeout,
        Action<WatchEventType, TEntity> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        CancellationToken cancellationToken = default,
        params ILabelSelector[] labelSelectors)
        where TEntity : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Create a entity watcher on the Kubernetes API.
    /// The entity watcher fires events for entity-events on
    /// Kubernetes (events: <see cref="WatchEventType"/>.
    /// </summary>
    /// <param name="timeout">The timeout which the watcher has (after this timeout, the server will close the connection).</param>
    /// <param name="onEvent">Action that is called when an event occurs.</param>
    /// <param name="onError">Action that handles exceptions.</param>
    /// <param name="onClose">Action that handles closed connections.</param>
    /// <param name="namespace">
    /// The namespace to watch for entities (if needed).
    /// If the namespace is omitted, all entities on the cluster are watched.
    /// </param>
    /// <param name="cancellationToken">Cancellation-Token.</param>
    /// <param name="labelSelector">A string, representing an optional label selector for filtering watched objects.</param>
    /// <typeparam name="TEntity">The concrete type of the entity.</typeparam>
    /// <returns>A entity watcher for the given entity.</returns>
    Task<Watcher<TEntity>> Watch<TEntity>(
        TimeSpan timeout,
        Action<WatchEventType, TEntity> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        CancellationToken cancellationToken = default,
        string? labelSelector = null)
        where TEntity : IKubernetesObject<V1ObjectMeta>;
}
