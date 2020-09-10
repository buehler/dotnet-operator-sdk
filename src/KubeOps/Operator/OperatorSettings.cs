using System.Reflection;
using System.Text.RegularExpressions;

namespace KubeOps.Operator
{
    public sealed class OperatorSettings
    {
        private const string DefaultOperatorName = "KubernetesOperator";
        private const string NonCharReplacement = "-";

        public string Name { get; set; } =
            new Regex(@"(\W|_)", RegexOptions.CultureInvariant).Replace(
                Assembly.GetEntryAssembly()?.GetName().Name ?? DefaultOperatorName, NonCharReplacement)
                .ToLowerInvariant();

        public string MetricsEndpoint { get; set; } = "/metrics";

        public string LivenessEndpoint { get; set; } = "/health";

        public string ReadinessEndpoint { get; set; } = "/ready";

        public ushort LeaderElectionCheckInterval { get; set; } = 15;

        public ushort LeaderElectionLeaseDuration { get; set; } = 30;
    }
}
