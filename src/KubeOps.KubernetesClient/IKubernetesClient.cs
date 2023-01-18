using k8s;
using k8s.Models;
using KubeOps.KubernetesClient.LabelSelectors;

namespace KubeOps.KubernetesClient;

/// <summary>
/// Client for the Kubernetes API. Contains various methods to manage Kubernetes resources.
/// </summary>
public interface IKubernetesClient
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
    /// Fetch and return the actual Kubernetes <see cref="VersionInfo"/> (aka. Server Version).
    /// </summary>
    /// <returns>The <see cref="VersionInfo"/> of the current server.</returns>
    Task<VersionInfo> GetServerVersion();

    /// <summary>
    /// Fetch and return a resource from the Kubernetes API.
    /// </summary>
    /// <param name="name">The name of the resource (metadata.name).</param>
    /// <param name="namespace">
    /// Optional namespace. If this is set, the resource must be a namespaced resource.
    /// If it is omitted, the resource must be a cluster wide resource.
    /// </param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>The found resource of the given type, or null otherwise.</returns>
    Task<TResource?> Get<TResource>(string name, string? @namespace = null)
        where TResource : class, IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Fetch and return a list of resources from the Kubernetes API.
    /// </summary>
    /// <param name="namespace">If the resources are namespaced, provide the name of the namespace.</param>
    /// <param name="labelSelector">A string, representing an optional label selector for filtering fetched objects.</param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>A list of Kubernetes resources.</returns>
    Task<IList<TResource>> List<TResource>(
        string? @namespace = null,
        string? labelSelector = null)
        where TResource : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Fetch and return a list of resources from the Kubernetes API.
    /// </summary>
    /// <param name="namespace">
    /// If only resources in a given namespace should be listed, provide the namespace here.
    /// </param>
    /// <param name="labelSelectors">A list of label-selectors to apply to the search.</param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>A list of Kubernetes resources.</returns>
    Task<IList<TResource>> List<TResource>(
        string? @namespace = null,
        params ILabelSelector[] labelSelectors)
        where TResource : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Create or Update a resource. If the resource already has an ID set the resource is updated,
    /// otherwise it is created.
    /// </summary>
    /// <param name="resource">The resource in question.</param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>The saved instance of the resource.</returns>
    Task<TResource> Save<TResource>(TResource resource)
        where TResource : class, IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Create the given resource on the Kubernetes API.
    /// </summary>
    /// <param name="resource">The resource instance.</param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>The created instance of the resource.</returns>
    Task<TResource> Create<TResource>(TResource resource)
        where TResource : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Update the given resource on the Kubernetes API.
    /// </summary>
    /// <param name="resource">The resource instance.</param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>The updated instance of the resource.</returns>
    Task<TResource> Update<TResource>(TResource resource)
        where TResource : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Update the status object of a given resource on the Kubernetes API.
    /// </summary>
    /// <param name="resource">The resource that contains a status object.</param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>A task that completes when the call was made.</returns>
    public Task UpdateStatus<TResource>(TResource resource)
        where TResource : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Delete a given resource from the Kubernetes API.
    /// </summary>
    /// <param name="resource">The resource in question.</param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>A task that completes when the call was made.</returns>
    Task Delete<TResource>(TResource resource)
        where TResource : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Delete a given list of resources from the Kubernetes API.
    /// </summary>
    /// <param name="resources">The resources in question.</param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>A task that completes when the calls were made.</returns>
    Task Delete<TResource>(IEnumerable<TResource> resources)
        where TResource : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Delete a given list of resources from the Kubernetes API.
    /// </summary>
    /// <param name="resources">The resources in question.</param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>A task that completes when the calls were made.</returns>
    Task Delete<TResource>(params TResource[] resources)
        where TResource : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Delete a given resource by name from the Kubernetes API.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="namespace">The optional namespace of the resource.</param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>A task that completes when the call was made.</returns>
    Task Delete<TResource>(string name, string? @namespace = null)
        where TResource : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Create a resource watcher on the Kubernetes API.
    /// The resource watcher fires events for resource-events on
    /// Kubernetes (events: <see cref="WatchEventType"/>.
    /// </summary>
    /// <param name="timeout">The timeout which the watcher has (after this timeout, the server will close the connection).</param>
    /// <param name="onEvent">Action that is called when an event occurs.</param>
    /// <param name="onError">Action that handles exceptions.</param>
    /// <param name="onClose">Action that handles closed connections.</param>
    /// <param name="namespace">
    /// The namespace to watch for resources (if needed).
    /// If the namespace is omitted, all resources on the cluster are watched.
    /// </param>
    /// <param name="cancellationToken">Cancellation-Token.</param>
    /// <param name="labelSelectors">A list of label-selectors to apply to the search.</param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>A resource watcher for the given resource.</returns>
    Task<Watcher<TResource>> Watch<TResource>(
        TimeSpan timeout,
        Action<WatchEventType, TResource> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        CancellationToken cancellationToken = default,
        params ILabelSelector[] labelSelectors)
        where TResource : IKubernetesObject<V1ObjectMeta>;

    /// <summary>
    /// Create a resource watcher on the Kubernetes API.
    /// The resource watcher fires events for resource-events on
    /// Kubernetes (events: <see cref="WatchEventType"/>.
    /// </summary>
    /// <param name="timeout">The timeout which the watcher has (after this timeout, the server will close the connection).</param>
    /// <param name="onEvent">Action that is called when an event occurs.</param>
    /// <param name="onError">Action that handles exceptions.</param>
    /// <param name="onClose">Action that handles closed connections.</param>
    /// <param name="namespace">
    /// The namespace to watch for resources (if needed).
    /// If the namespace is omitted, all resources on the cluster are watched.
    /// </param>
    /// <param name="cancellationToken">Cancellation-Token.</param>
    /// <param name="labelSelector">A string, representing an optional label selector for filtering watched objects.</param>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>A resource watcher for the given resource.</returns>
    Task<Watcher<TResource>> Watch<TResource>(
        TimeSpan timeout,
        Action<WatchEventType, TResource> onEvent,
        Action<Exception>? onError = null,
        Action? onClose = null,
        string? @namespace = null,
        CancellationToken cancellationToken = default,
        string? labelSelector = null)
        where TResource : IKubernetesObject<V1ObjectMeta>;
}
