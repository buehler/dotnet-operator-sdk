using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using DotnetKubernetesClient;
using KubeOps.Operator.Serialization;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KubeOps.Operator
{
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

        internal JsonSerializerSettings SerializerSettings { get; } = new()
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new NamingConvention(),
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter { CamelCaseText = true },
                new Iso8601TimeSpanConverter(),
            },
            DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffffffK",
        };
    }
}
