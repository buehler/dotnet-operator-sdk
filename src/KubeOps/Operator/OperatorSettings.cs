namespace KubeOps.Operator
{
    public sealed class OperatorSettings
    {
        public string Name { get; set; } = string.Empty;

        public short Port { get; set; } = 80;

        public string MetricsEndpoint { get; set; } = "/metrics";

        public string LivenessEndpoint { get; set; } = "/health";

        public string ReadinessEndpoint { get; set; } = "/ready";
    }
}
