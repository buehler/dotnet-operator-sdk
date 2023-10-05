using System.Text.RegularExpressions;

namespace KubeOps.Abstractions.Builder;

/// <summary>
/// Operator settings.
/// </summary>
public sealed class OperatorSettings
{
    private const string DefaultOperatorName = "KubernetesOperator";
    private const string NonCharReplacement = "-";

    /// <summary>
    /// The name of the operator that appears in logs and other elements.
    /// Defaults to "kubernetesoperator" when not set.
    /// </summary>
    public string Name { get; set; } =
        new Regex(@"(\W|_)", RegexOptions.CultureInvariant).Replace(
                DefaultOperatorName,
                NonCharReplacement)
            .ToLowerInvariant();

    /// <summary>
    /// <para>
    /// Controls the namespace which is watched by the operator.
    /// If this field is left `null`, all namespaces are watched for
    /// CRD instances.
    /// </para>
    /// </summary>
    public string? Namespace { get; set; }

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
    /// The interval in seconds in which this particular instance of the operator
    /// will check for leader election.
    /// </summary>
    public ushort LeaderElectionCheckInterval { get; set; } = 15;

    /// <summary>
    /// The duration in seconds in which the leader lease is valid.
    /// </summary>
    public ushort LeaderElectionLeaseDuration { get; set; } = 30;
}
