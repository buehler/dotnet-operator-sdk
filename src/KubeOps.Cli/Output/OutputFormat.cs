namespace KubeOps.Cli.Output;

internal enum OutputFormat
{
    /// <summary>
    /// Format the output in Kubernetes YAML style.
    /// </summary>
    Yaml,

    /// <summary>
    /// Format the output in Kubernetes JSON style.
    /// </summary>
    Json,
}
