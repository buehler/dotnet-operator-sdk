using System.Net;
using k8s.Autorest;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.KubernetesClient.LabelSelectors;
using KubeOps.Operator.Entities.Extensions;
using Timer = System.Timers.Timer;

namespace KubeOps.Operator.Leadership;

internal class LeaderElector : IHostedService
{
    private readonly ILogger<LeaderElector> _logger;
    private readonly OperatorSettings _settings;
    private readonly ILeaderElection _election;
    private readonly IKubernetesClient _client;

    private readonly string _leaseName;
    private readonly string _hostname;

    private Timer? _leaseCheck;
    private string _namespace = string.Empty;
    private V1Deployment? _operatorDeployment;

    public LeaderElector(
        ILogger<LeaderElector> logger,
        OperatorSettings settings,
        ILeaderElection election,
        IKubernetesClient client)
    {
        _logger = logger;
        _settings = settings;
        _election = election;
        _client = client;

        _leaseName = $"{_settings.Name}-leadership";
        _hostname = Environment.MachineName;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(@"Startup Leader Elector for operator ""{operatorName}"".", _settings.Name);

        _leaseCheck?.Dispose();
        _leaseCheck = new Timer(
            TimeSpan.FromSeconds(_settings.LeaderElectionCheckInterval).TotalMilliseconds) { AutoReset = true, };

        _logger.LogTrace("Fetching namespace for leader election.");
        _namespace = await _client.GetCurrentNamespace();
        _operatorDeployment = (await _client.List<V1Deployment>(
            _namespace,
            new EqualsSelector("operator-deployment", _settings.Name))).FirstOrDefault();
        if (_operatorDeployment != null)
        {
            _operatorDeployment.Kind = V1Deployment.KubeKind;
            _operatorDeployment.ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}";
        }

#if DEBUG
        _election.LeadershipChanged(LeaderState.Leader);
#else
        _leaseCheck.Start();
        _leaseCheck.Elapsed += async (_, __) => await CheckLeaderLease();

        await CheckLeaderLease();
#endif
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(@"Shutdown Leader Elector for operator ""{operatorName}"".", _settings.Name);
        _leaseCheck?.Dispose();
        _leaseCheck = null;

        _namespace = string.Empty;
        _operatorDeployment = null;

        return ClearLeaseIfLeader();
    }

    /// <summary>
    /// Check the <see cref="V1Lease"/> object for the leader election.
    /// </summary>
    /// <returns>A Task.</returns>
    internal async Task CheckLeaderLease()
    {
        _logger.LogTrace(@"Fetch V1Lease object for operator ""{operator}"".", _settings.Name);
        var lease = await _client.Get<V1Lease>(_leaseName, _namespace);

        // If the lease does not exist, create it, set this instance as leader
        // fire the appropriate event, return.
        if (lease == null)
        {
            _logger.LogInformation(
                @"There was no lease for operator ""{operator}"". Creating one and electing ""{hostname}"" as leader.",
                _settings.Name,
                _hostname);
            try
            {
                await _client.Create(
                    new V1Lease(
                        $"{V1Lease.KubeGroup}/{V1Lease.KubeApiVersion}",
                        V1Lease.KubeKind,
                        new V1ObjectMeta(
                            name: _leaseName,
                            namespaceProperty: _namespace,
                            ownerReferences: _operatorDeployment != null
                                ? new List<V1OwnerReference> { _operatorDeployment.MakeOwnerReference(), }
                                : null,
                            annotations: new Dictionary<string, string> { { "leader-elector", _settings.Name } }),
                        new V1LeaseSpec(
                            DateTime.UtcNow,
                            _hostname,
                            _settings.LeaderElectionLeaseDuration,
                            0,
                            DateTime.UtcNow)));
                _election.LeadershipChanged(LeaderState.Leader);
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.LogInformation("Another instance of the operator was faster. Falling back to candiate.");
                _election.LeadershipChanged(LeaderState.Candidate);
            }
            catch (HttpOperationException ex)
            {
                _logger.LogCritical(
                    ex,
                    @"A http error happened during leader election check of instance ""{hostname}"" of operator ""{operator}"". Response Message: ""{response}""",
                    _hostname,
                    _settings.Name,
                    $"Phrase: {ex.Response.ReasonPhrase}\nContent: {ex.Response.Content}");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(
                    ex,
                    @"A generic error happened during leader election check of instance ""{hostname}"" of operator ""{operator}"".",
                    _hostname,
                    _settings.Name);
            }

            return;
        }

        /*
        If the lease exists, check if this instance is the leader.
        If it is, update the renew time, and update the entity.
        If it isn't and the lease time is in the past,
        set the leader, update the entity, trigger event.
        */

        if (lease.Spec.HolderIdentity == _hostname)
        {
            _logger.LogDebug(
                @"The instance ""{hostname}"" is still the leader for operator ""{operator}"".",
                _hostname,
                _settings.Name);
            lease.Spec.RenewTime = DateTime.UtcNow;

            try
            {
                await _client.Update(lease);
                _election.LeadershipChanged(LeaderState.Leader);
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.LogWarning("Another instance updated the lease. Retry on next cycle.");
            }
            catch (HttpOperationException ex)
            {
                _logger.LogCritical(
                    ex,
                    @"A http error happened during leader election check of instance ""{hostname}"" of operator ""{operator}"". Response Message: ""{response}""",
                    _hostname,
                    _settings.Name,
                    $"Phrase: {ex.Response.ReasonPhrase}\nContent: {ex.Response.Content}");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(
                    ex,
                    @"A generic error happened during leader election check of instance ""{hostname}"" of operator ""{operator}"".",
                    _hostname,
                    _settings.Name);
            }

            return;
        }

        if (lease.Spec.RenewTime.HasValue &&
            lease.Spec.RenewTime.Value +
            TimeSpan.FromSeconds(lease.Spec.LeaseDurationSeconds ?? _settings.LeaderElectionLeaseDuration) <
            DateTime.UtcNow)
        {
            _logger.LogInformation(
                @"The lease for operator ""{operator}"" ran out. Electing ""{hostname}"" as leader.",
                _settings.Name,
                _hostname);

            lease.Spec.AcquireTime = DateTime.UtcNow;
            lease.Spec.RenewTime = DateTime.UtcNow;
            lease.Spec.HolderIdentity = _hostname;
            lease.Spec.LeaseTransitions ??= 0;
            lease.Spec.LeaseTransitions += 1;

            try
            {
                await _client.Update(lease);
                _election.LeadershipChanged(LeaderState.Leader);
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.Conflict)
            {
                _logger.LogWarning("Another instance updated the lease. Retry on next cycle.");
            }
            catch (HttpOperationException ex)
            {
                _logger.LogCritical(
                    ex,
                    @"A http error happened during leader election check of instance ""{hostname}"" of operator ""{operator}"". Response Message: ""{response}""",
                    _hostname,
                    _settings.Name,
                    $"Phrase: {ex.Response.ReasonPhrase}\nContent: {ex.Response.Content}");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(
                    ex,
                    @"A generic error happened during leader election check of instance ""{hostname}"" of operator ""{operator}"".",
                    _hostname,
                    _settings.Name);
            }

            return;
        }

        _logger.LogDebug(
            @"The lease for operator ""{operator}"" did not ran out, staying/becoming candidate.",
            _settings.Name);
        _election.LeadershipChanged(LeaderState.Candidate);
    }

    /// <summary>
    /// If this instance is the leader, delete the V1Lease object.
    /// This way, a new leader can take place, and if all instances
    /// are shut down, the last instance will kill the lease object
    /// and therefore suppress orphans.
    /// </summary>
    /// <returns>A Task.</returns>
    internal async Task ClearLeaseIfLeader()
    {
        if (_namespace.Length == 0)
        {
            _logger.LogTrace("Fetching namespace for leader election.");
            _namespace = await _client.GetCurrentNamespace();
        }

        _logger.LogTrace(@"Fetch V1Lease object for operator ""{operator}"".", _settings.Name);
        var lease = await _client.Get<V1Lease>(_leaseName, _namespace);

        if (lease == null || lease.Spec.HolderIdentity != _hostname)
        {
            return;
        }

        _logger.LogInformation(
            @"Shutting down instance ""{hostname}"". Deleting the lease for operator ""{operator}"".",
            _hostname,
            _settings.Name);
        await _client.Delete(lease);
    }
}
