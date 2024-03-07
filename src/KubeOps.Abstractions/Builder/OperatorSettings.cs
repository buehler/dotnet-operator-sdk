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
    /// Whether the leader elector should run. You should enable
    /// this if you plan to run the operator redundantly.
    /// </para>
    /// <para>
    /// If this is disabled and an operator runs in multiple instances
    /// (in the same namespace), it can lead to a "split brain" problem.
    /// </para>
    /// <para>
    /// Defaults to `false`.
    /// </para>
    /// </summary>
    public bool EnableLeaderElection { get; set; } = false;

    /// <summary>
    /// Defines how long one lease is valid for any leader.
    /// Defaults to 15 seconds.
    /// </summary>
    public TimeSpan LeaderElectionLeaseDuration { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// When the leader elector tries to refresh the leadership lease.
    /// </summary>
    public TimeSpan LeaderElectionRenewDeadline { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The wait timeout if the lease cannot be acquired.
    /// </summary>
    public TimeSpan LeaderElectionRetryPeriod { get; set; } = TimeSpan.FromSeconds(2);
}
