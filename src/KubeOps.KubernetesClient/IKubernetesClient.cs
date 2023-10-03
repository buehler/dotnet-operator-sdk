using k8s;
using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.KubernetesClient.LabelSelectors;

namespace KubeOps.KubernetesClient;

// TODO: make all sync calls as well.
// TODO: test list / get call.
// TODO: update list call

/// <summary>
/// Client for the Kubernetes API. Contains various methods to manage Kubernetes entities.
/// This client is specific to an entity of type <typeparamref name="TEntity"/>.
/// </summary>
/// <typeparam name="TEntity">The type of the Kubernetes entity.</typeparam>
public interface IKubernetesClient<TEntity> : IDisposable
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
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
    /// Fetch and return an entity from the Kubernetes API.
    /// </summary>
    /// <param name="name">The name of the entity (metadata.name).</param>
    /// <param name="namespace">
    /// Optional namespace. If this is set, the entity must be a namespaced entity.
    /// If it is omitted, the entity must be a cluster wide entity.
    /// </param>
    /// <returns>The found entity of the given type, or null otherwise.</returns>
    Task<TEntity?> Get(string name, string? @namespace = null);

    /// <summary>
    /// Fetch and return a list of entities from the Kubernetes API.
    /// </summary>
    /// <param name="namespace">If the entities are namespaced, provide the name of the namespace.</param>
    /// <param name="labelSelector">A string, representing an optional label selector for filtering fetched objects.</param>
    /// <returns>A list of Kubernetes entities.</returns>
    Task<IList<TEntity>> List(
        string? @namespace = null,
        string? labelSelector = null);

    /// <summary>
    /// Fetch and return a list of entities from the Kubernetes API.
    /// </summary>
    /// <param name="namespace">
    /// If only entities in a given namespace should be listed, provide the namespace here.
    /// </param>
    /// <param name="labelSelectors">A list of label-selectors to apply to the search.</param>
    /// <returns>A list of Kubernetes entities.</returns>
    Task<IList<TEntity>> List(
        string? @namespace = null,
        params LabelSelector[] labelSelectors);

    /// <summary>
    /// Create or Update a entity. This first fetches the entity from the Kubernetes API
    /// and if it does exist, updates the entity. Otherwise, the entity is created.
    /// </summary>
    /// <param name="entity">The entity in question.</param>
    /// <returns>The saved instance of the entity.</returns>
    async Task<TEntity> Save(TEntity entity) => await Get(entity.Name(), entity.Namespace()) switch
    {
        { } e => await Update(entity.WithResourceVersion(e)),
        _ => await Create(entity),
    };

    /// <summary>
    /// Create the given entity on the Kubernetes API.
    /// </summary>
    /// <param name="entity">The entity instance.</param>
    /// <returns>The created instance of the entity.</returns>
    Task<TEntity> Create(TEntity entity);

    /// <summary>
    /// Update the given entity on the Kubernetes API.
    /// </summary>
    /// <param name="entity">The entity instance.</param>
    /// <returns>The updated instance of the entity.</returns>
    Task<TEntity> Update(TEntity entity);

    /// <summary>
    /// Update the status object of a given entity on the Kubernetes API.
    /// </summary>
    /// <param name="entity">The entity that contains a status object.</param>
    /// <returns>A task that completes when the call was made.</returns>
    public Task<TEntity> UpdateStatus(TEntity entity);

    /// <summary>
    /// Delete a given entity from the Kubernetes API.
    /// </summary>
    /// <param name="entity">The entity in question.</param>
    /// <returns>A task that completes when the call was made.</returns>
    Task Delete(TEntity entity);

    /// <summary>
    /// Delete a given list of entities from the Kubernetes API.
    /// </summary>
    /// <param name="entities">The entities in question.</param>
    /// <returns>A task that completes when the calls were made.</returns>
    Task Delete(IEnumerable<TEntity> entities);

    /// <summary>
    /// Delete a given list of entities from the Kubernetes API.
    /// </summary>
    /// <param name="entities">The entities in question.</param>
    /// <returns>A task that completes when the calls were made.</returns>
    Task Delete(params TEntity[] entities);

    /// <summary>
    /// Delete a given entity by name from the Kubernetes API.
    /// </summary>
    /// <param name="name">The name of the entity.</param>
    /// <param name="namespace">The optional namespace of the entity.</param>
    /// <returns>A task that completes when the call was made.</returns>
    Task Delete(string name, string? @namespace = null);

    /// <summary>
    /// Create a entity watcher on the Kubernetes API.
    /// The entity watcher fires events for entity-events on
    /// Kubernetes (events: <see cref="WatchEventType"/>.
    /// </summary>
    /// <param name="onEvent">Action that is called when an event occurs.</param>
    /// <param name="onError">Action that handles exceptions.</param>
    /// <param name="onClose">Action that handles closed connections.</param>
    /// <param name="namespace">
    /// The namespace to watch for entities (if needed).
    /// If the namespace is omitted, all entities on the cluster are watched.
    /// </param>
    /// <param name="timeout">The timeout which the watcher has (after this timeout, the server will close the connection).</param>
    /// <param name="cancellationToken">Cancellation-Token.</param>
    /// <param name="labelSelectors">A list of label-selectors to apply to the search.</param>
    /// <returns>A entity watcher for the given entity.</returns>
    Watcher<TEntity> Watch(
        Action<WatchEventType, TEntity> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default,
        params LabelSelector[] labelSelectors);

    /// <summary>
    /// Create a entity watcher on the Kubernetes API.
    /// The entity watcher fires events for entity-events on
    /// Kubernetes (events: <see cref="WatchEventType"/>.
    /// </summary>
    /// <param name="onEvent">Action that is called when an event occurs.</param>
    /// <param name="onError">Action that handles exceptions.</param>
    /// <param name="onClose">Action that handles closed connections.</param>
    /// <param name="namespace">
    /// The namespace to watch for entities (if needed).
    /// If the namespace is omitted, all entities on the cluster are watched.
    /// </param>
    /// <param name="timeout">The timeout which the watcher has (after this timeout, the server will close the connection).</param>
    /// <param name="labelSelector">A string, representing an optional label selector for filtering watched objects.</param>
    /// <param name="cancellationToken">Cancellation-Token.</param>
    /// <returns>A entity watcher for the given entity.</returns>
    Watcher<TEntity> Watch(
        Action<WatchEventType, TEntity> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        TimeSpan? timeout = null,
        string? labelSelector = null,
        CancellationToken cancellationToken = default);
}
