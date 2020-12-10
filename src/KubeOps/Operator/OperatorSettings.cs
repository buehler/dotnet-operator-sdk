using System.Reflection;
using System.Text.RegularExpressions;
using DotnetKubernetesClient;

namespace KubeOps.Operator
{
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
    }
}
