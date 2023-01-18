using System.Reflection;
using System.Text.RegularExpressions;
using KellermanSoftware.CompareNetObjects;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Errors;

namespace KubeOps.Operator;

/// <summary>
/// Operator settings.
/// </summary>
public sealed class OperatorSettings
{
    private const string DefaultOperatorName = "KubernetesOperator";
    private const string NonCharReplacement = "-";

    /// <summary>
    /// The name of the operator that appears in logs and other elements.
    /// </summary>
    public string Name { get; set; } =
        new Regex(@"(\W|_)", RegexOptions.CultureInvariant).Replace(
                Assembly.GetEntryAssembly()?.GetName().Name ?? DefaultOperatorName,
                NonCharReplacement)
            .ToLowerInvariant();

    /// <summary>
    /// <para>
    /// Controls the namespace which is watched by the operator.
    /// If this field is left `null`, all namespaces are watched for
    /// CRD instances.
    /// </para>
    /// <para>
    /// The namespace could be passed to the software via environment
    /// variable or can be fetched via the <see cref="IKubernetesClient.GetCurrentNamespace"/>
    /// method of the <see cref="IKubernetesClient"/>.
    /// </para>
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// The maximal number of retries for an error during reconciliation.
    /// The controller skips the reconciliation event if an entity throws errors during
    /// reconciliation. Depending on the <see cref="ErrorBackoffStrategy"/>, this could
    /// result in endless waiting times.
    /// </summary>
    public int MaxErrorRetries { get; set; } = 4;

    /// <summary>
    /// The maximal number of seconds that the resource watcher waits until it retries to connect to Kubernetes.
    /// The amount of time is determined by <see cref="ErrorBackoffStrategy"/> and the minimal value of
    /// the calculated one and this configuration is used.
    /// </summary>
    public int WatcherMaxRetrySeconds { get; set; } = 32;

    /// <summary>
    /// Configures the <see cref="BackoffStrategy"/> for error events during reconciliation.
    /// When the controller faces an error, it waits for the returned amount of time of this strategy
    /// and retries until the controller drops the event configured by <see cref="MaxErrorRetries"/>.
    /// </summary>
    public BackoffStrategy ErrorBackoffStrategy { get; set; } = BackoffStrategies.ExponentialBackoffStrategy;

    /// <summary>
    /// The http endpoint, where the metrics are exposed.
    /// </summary>
    public string MetricsEndpoint { get; set; } = "/metrics";

    /// <summary>
    /// The http endpoint, where the liveness probes are exposed.
    /// </summary>
    public string LivenessEndpoint { get; set; } = "/health";

    /// <summary>
    /// The http endpoint, where the readiness probes are exposed.
    /// </summary>
    public string ReadinessEndpoint { get; set; } = "/ready";

    /// <summary>
    /// <para>
    /// Defines if the leader elector should run. You may disable this,
    /// if you don't intend to run your operator multiple times.
    /// </para>
    /// <para>
    /// If this is disabled, and an operator runs in multiple instance
    /// (in the same namespace) it can lead to a "split brain" problem.
    /// </para>
    /// <para>
    /// This could be disabled when developing locally.
    /// </para>
    /// </summary>
    public bool EnableLeaderElection { get; set; } = true;

    /// <summary>
    /// <para>
    /// If set to true, controllers will only watch for new events when in a leader state,
    /// or if leadership is disabled. When false, this check is disabled,
    /// controllers will always watch for resource changes regardless of leadership state.
    /// </para>
    /// <para>
    /// If this is disabled, you should consider checking leadership state manually,
    /// to prevent a "split brain" problem.
    /// </para>
    /// <para>
    /// Defaults to true.
    /// </para>
    /// </summary>
    public bool OnlyWatchEventsWhenLeader { get; set; } = true;

    /// <summary>
    /// The interval in seconds in which this particular instance of the operator
    /// will check for leader election.
    /// </summary>
    public ushort LeaderElectionCheckInterval { get; set; } = 15;

    /// <summary>
    /// The duration in seconds in which the leader lease is valid.
    /// </summary>
    public ushort LeaderElectionLeaseDuration { get; set; } = 30;

    /// <summary>
    /// The timeout in seconds which the watcher has (after this timeout, the server will close the connection).
    /// </summary>
    public ushort WatcherHttpTimeout { get; set; } = 60;

    /// <summary>
    /// <para>
    /// If set to true, controllers perform a search for already
    /// existing objects in the cluster and load them into the objects cache.
    /// </para>
    /// <para>
    /// This bears the risk of not catching elements when they are created
    /// during downtime of the operator.
    /// </para>
    /// <para>The search will be performed on each "Start" of the controller.</para>
    /// </summary>
    public bool PreloadCache { get; set; }

    /// <summary>
    /// <para>
    /// If set to true, returning `ResourceControllerResult.RequeueEvent` will
    /// automatically requeue the event as the same type.
    /// </para>
    /// <para>
    /// For example, if done from a "Created" event, the event will be queued
    /// again as "Created" instead of (for example) "NotModified".
    /// </para>
    /// </summary>
    public bool DefaultRequeueAsSameType { get; set; } = false;

    /// <summary>
    /// <para>
    /// If set to true, the executing assembly will be scanned for controllers,
    /// finalizers, mutators and validators.
    /// </para>
    /// </summary>
    public bool EnableAssemblyScanning { get; set; } = true;

    /// <summary>
    /// The configured http port that the operator should run
    /// on Kubernetes. This has no direct impact on the startup call in `Program.cs`,
    /// but on the generated yaml files of the operator. This setting modifies
    /// the environment variable "KESTREL__ENDPOINTS__HTTP__URL" in the yaml file.
    /// </summary>
    public short HttpPort { get; set; } = 5000;

    /// <summary>
    /// The configured https port that the operator should run
    /// on Kubernetes. This has no direct impact on the startup call in `Program.cs`,
    /// but on the generated yaml files of the operator. This setting modifies
    /// the environment variable "KESTREL__ENDPOINTS__HTTPS__URL" in the yaml file.
    /// </summary>
    public short HttpsPort { get; set; } = 5001;

    /// <summary>
    /// The configuration used when comparing resources against each-other for caching
    /// or other similar processing.
    /// </summary>
    public ComparisonConfig CacheComparisonConfig { get; set; } = new()
    {
        Caching = true,
        AutoClearCache = false,
        MembersToIgnore = new List<string> { "ResourceVersion", "ManagedFields" },
    };
}
