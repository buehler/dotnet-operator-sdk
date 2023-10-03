using k8s;
using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.KubernetesClient.LabelSelectors;

namespace KubeOps.KubernetesClient;

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
    Task<string> GetCurrentNamespaceAsync(string downwardApiEnvName = "POD_NAMESPACE");

    /// <inheritdoc cref="GetCurrentNamespaceAsync" />
    string GetCurrentNamespace(string downwardApiEnvName = "POD_NAMESPACE");

    /// <summary>
    /// Fetch and return an entity from the Kubernetes API.
    /// </summary>
    /// <param name="name">The name of the entity (metadata.name).</param>
    /// <param name="namespace">
    /// Optional namespace. If this is set, the entity must be a namespaced entity.
    /// If it is omitted, the entity must be a cluster wide entity.
    /// </param>
    /// <returns>The found entity of the given type, or null otherwise.</returns>
    Task<TEntity?> GetAsync(string name, string? @namespace = null);

    /// <inheritdoc cref="GetAsync"/>
    TEntity? Get(string name, string? @namespace = null);

    /// <summary>
    /// Fetch and return a list of entities from the Kubernetes API.
    /// </summary>
    /// <param name="namespace">If the entities are namespaced, provide the name of the namespace.</param>
    /// <param name="labelSelector">A string, representing an optional label selector for filtering fetched objects.</param>
    /// <returns>A list of Kubernetes entities.</returns>
    Task<IList<TEntity>> ListAsync(
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
    Task<IList<TEntity>> ListAsync(
        string? @namespace = null,
        params LabelSelector[] labelSelectors)
        => ListAsync(@namespace, labelSelectors.ToExpression());

    /// <inheritdoc cref="ListAsync(string?,string?)"/>
    IList<TEntity> List(
        string? @namespace = null,
        string? labelSelector = null);

    /// <inheritdoc cref="ListAsync(string?,LabelSelector[])"/>
    IList<TEntity> List(
        string? @namespace = null,
        params LabelSelector[] labelSelectors)
        => List(@namespace, labelSelectors.ToExpression());

    /// <summary>
    /// Create or Update a entity. This first fetches the entity from the Kubernetes API
    /// and if it does exist, updates the entity. Otherwise, the entity is created.
    /// </summary>
    /// <param name="entity">The entity in question.</param>
    /// <returns>The saved instance of the entity.</returns>
    async Task<TEntity> SaveAsync(TEntity entity) => await GetAsync(entity.Name(), entity.Namespace()) switch
    {
        { } e => await UpdateAsync(entity.WithResourceVersion(e)),
        _ => await CreateAsync(entity),
    };

    /// <summary>
    /// Create or Update a list of entities. This first fetches each entity from the Kubernetes API
    /// and if it does exist, updates the entity. Otherwise, the entity is created.
    /// </summary>
    /// <param name="entities">The entity list.</param>
    /// <returns>The saved instances of the entities.</returns>
    async Task<IEnumerable<TEntity>> SaveAsync(IEnumerable<TEntity> entities) =>
        await Task.WhenAll(entities.Select(SaveAsync));

    /// <summary>
    /// Create or Update a list of entities. This first fetches each entity from the Kubernetes API
    /// and if it does exist, updates the entity. Otherwise, the entity is created.
    /// </summary>
    /// <param name="entities">The entity list.</param>
    /// <returns>The saved instances of the entities.</returns>
    async Task<IEnumerable<TEntity>> SaveAsync(params TEntity[] entities) =>
        await Task.WhenAll(entities.Select(SaveAsync));

    /// <inheritdoc cref="SaveAsync(TEntity)"/>
    TEntity Save(TEntity entity) => Get(entity.Name(), entity.Namespace()) switch
    {
        { } e => Update(entity.WithResourceVersion(e)),
        _ => Create(entity),
    };

    /// <inheritdoc cref="SaveAsync(IEnumerable{TEntity})"/>
    IEnumerable<TEntity> Save(IEnumerable<TEntity> entities) => entities.Select(Save);

    /// <inheritdoc cref="SaveAsync(IEnumerable{TEntity})"/>
    IEnumerable<TEntity> Save(params TEntity[] entities) => entities.Select(Save);

    /// <summary>
    /// Create the given entity on the Kubernetes API.
    /// </summary>
    /// <param name="entity">The entity instance.</param>
    /// <returns>The created instance of the entity.</returns>
    Task<TEntity> CreateAsync(TEntity entity);

    /// <summary>
    /// Create a list of entities on the Kubernetes API.
    /// </summary>
    /// <param name="entities">The entity list.</param>
    /// <returns>The created instances of the entities.</returns>
    async Task<IEnumerable<TEntity>> CreateAsync(IEnumerable<TEntity> entities)
        => await Task.WhenAll(entities.Select(CreateAsync));

    /// <summary>
    /// Create a list of entities on the Kubernetes API.
    /// </summary>
    /// <param name="entities">The entity list.</param>
    /// <returns>The created instances of the entities.</returns>
    async Task<IEnumerable<TEntity>> CreateAsync(params TEntity[] entities)
        => await Task.WhenAll(entities.Select(CreateAsync));

    /// <inheritdoc cref="CreateAsync(TEntity)"/>
    TEntity Create(TEntity entity)
        => CreateAsync(entity).GetAwaiter().GetResult();

    /// <inheritdoc cref="CreateAsync(IEnumerable{TEntity})"/>
    IEnumerable<TEntity> Create(IEnumerable<TEntity> entities)
        => entities.Select(Create);

    /// <inheritdoc cref="CreateAsync(TEntity[])"/>
    IEnumerable<TEntity> Create(params TEntity[] entities)
        => entities.Select(Create);

    /// <summary>
    /// Update the given entity on the Kubernetes API.
    /// </summary>
    /// <param name="entity">The entity instance.</param>
    /// <returns>The updated instance of the entity.</returns>
    Task<TEntity> UpdateAsync(TEntity entity);

    /// <summary>
    /// Update a list of entities on the Kubernetes API.
    /// </summary>
    /// <param name="entities">An enumerable of entities.</param>
    /// <returns>The updated instances of the entities.</returns>
    async Task<IEnumerable<TEntity>> UpdateAsync(IEnumerable<TEntity> entities)
        => await Task.WhenAll(entities.Select(UpdateAsync));

    /// <summary>
    /// Update a list of entities on the Kubernetes API.
    /// </summary>
    /// <param name="entities">An enumerable of entities.</param>
    /// <returns>The updated instances of the entities.</returns>
    async Task<IEnumerable<TEntity>> UpdateAsync(params TEntity[] entities)
        => await Task.WhenAll(entities.Select(UpdateAsync));

    /// <inheritdoc cref="UpdateAsync(TEntity)"/>
    TEntity Update(TEntity entity)
        => UpdateAsync(entity).GetAwaiter().GetResult();

    /// <inheritdoc cref="UpdateAsync(IEnumerable{TEntity})"/>
    IEnumerable<TEntity> Update(IEnumerable<TEntity> entities)
        => entities.Select(Update);

    /// <inheritdoc cref="UpdateAsync(TEntity[])"/>
    IEnumerable<TEntity> Update(params TEntity[] entities)
        => entities.Select(Update);

    /// <summary>
    /// Update the status object of a given entity on the Kubernetes API.
    /// </summary>
    /// <param name="entity">The entity that contains a status object.</param>
    /// <returns>The entity with the updated status.</returns>
    Task<TEntity> UpdateStatusAsync(TEntity entity);

    /// <inheritdoc cref="UpdateStatusAsync"/>
    TEntity UpdateStatus(TEntity entity);

    /// <inheritdoc cref="Delete(TEntity)"/>
    /// <returns>A task that completes when the call was made.</returns>
    Task DeleteAsync(TEntity entity) => DeleteAsync(entity.Name(), entity.Namespace());

    /// <inheritdoc cref="Delete(IEnumerable{TEntity})"/>
    /// <returns>A task that completes when the call was made.</returns>
    Task DeleteAsync(IEnumerable<TEntity> entities)
        => Task.WhenAll(entities.Select(DeleteAsync));

    /// <inheritdoc cref="Delete(TEntity[])"/>
    /// <returns>A task that completes when the call was made.</returns>
    Task DeleteAsync(params TEntity[] entities)
        => Task.WhenAll(entities.Select(DeleteAsync));

    /// <inheritdoc cref="Delete(string,string?)"/>
    /// <returns>A task that completes when the call was made.</returns>
    Task DeleteAsync(string name, string? @namespace = null);

    /// <summary>
    /// Delete a given entity from the Kubernetes API.
    /// </summary>
    /// <param name="entity">The entity in question.</param>
    void Delete(TEntity entity) => Delete(entity.Name(), entity.Namespace());

    /// <summary>
    /// Delete a given list of entities from the Kubernetes API.
    /// </summary>
    /// <param name="entities">The entities in question.</param>
    void Delete(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            Delete(entity);
        }
    }

    /// <summary>
    /// Delete a given list of entities from the Kubernetes API.
    /// </summary>
    /// <param name="entities">The entities in question.</param>
    void Delete(params TEntity[] entities)
    {
        foreach (var entity in entities)
        {
            Delete(entity);
        }
    }

    /// <summary>
    /// Delete a given entity by name from the Kubernetes API.
    /// </summary>
    /// <param name="name">The name of the entity.</param>
    /// <param name="namespace">The optional namespace of the entity.</param>
    void Delete(string name, string? @namespace = null)
        => DeleteAsync(name, @namespace).GetAwaiter().GetResult();

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
        params LabelSelector[] labelSelectors)
        => Watch(
            onEvent,
            onError,
            onClose,
            @namespace,
            timeout,
            labelSelectors.ToExpression(),
            cancellationToken);

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
