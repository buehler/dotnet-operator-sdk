using System.Net;
using k8s.Autorest;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator;
using KubeOps.Operator.Leadership;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KubeOps.Test.Operator.Leadership;

public class LeaderElectorTest
{
    private readonly LeaderElector _elector;
    private readonly Mock<ILeaderElection> _election = new();
    private readonly Mock<IKubernetesClient> _client = new();
    private readonly Mock<ILogger<LeaderElector>> _logger = new();

    public LeaderElectorTest()
    {
        _elector = new LeaderElector(
            _logger.Object,
            new OperatorSettings { Name = "test" },
            _election.Object,
            _client.Object);
    }

    [Fact]
    public async Task Should_Elect_Leadership_By_Default()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        await _elector.CheckLeaderLease();
        _election.Verify(e => e.LeadershipChanged(It.IsAny<LeaderState>()), Times.Once);
    }

    [Fact]
    public async Task Should_Elect_Leadership_When_Lease_Is_Null()
    {
        _election.Setup(e => e.LeadershipChanged(LeaderState.Leader));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(() => null);
        _client.Setup(c => c.Create(It.IsAny<V1Lease>()));
        await _elector.CheckLeaderLease();
        _election.Verify(e => e.LeadershipChanged(LeaderState.Leader), Times.Once);
        _client.Verify(c => c.Create(It.IsAny<V1Lease>()), Times.Once);
    }

    [Fact]
    public async Task Should_Fallback_To_Candiate_On_Conflict_When_Lease_Is_Null()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(() => null);
        _client
            .Setup(c => c.Create(It.IsAny<V1Lease>()))
            .ThrowsAsync(
                new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(
                        new HttpResponseMessage(HttpStatusCode.Conflict),
                        string.Empty),
                });

        await _elector.CheckLeaderLease();

        _election.Verify(e => e.LeadershipChanged(LeaderState.Leader), Times.Never);
        _election.Verify(e => e.LeadershipChanged(LeaderState.Candidate), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Elect_Anything_On_Http_Exception_When_Lease_Is_Null()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(() => null);
        _client
            .Setup(c => c.Create(It.IsAny<V1Lease>()))
            .ThrowsAsync(
                new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(
                        new HttpResponseMessage(HttpStatusCode.BadGateway),
                        string.Empty),
                });

        await _elector.CheckLeaderLease();

        _election.Verify(e => e.LeadershipChanged(It.IsAny<LeaderState>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Elect_Anything_On_Exception_When_Lease_Is_Null()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(() => null);
        _client
            .Setup(c => c.Create(It.IsAny<V1Lease>()))
            .ThrowsAsync(
                new Exception());

        await _elector.CheckLeaderLease();

        _election.Verify(e => e.LeadershipChanged(It.IsAny<LeaderState>()), Times.Never);
    }

    [Fact]
    public async Task Should_Reelect_Itself_As_Leader()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(
                () => new V1Lease { Spec = new V1LeaseSpec { HolderIdentity = Environment.MachineName, }, });

        await _elector.CheckLeaderLease();

        _election.Verify(e => e.LeadershipChanged(LeaderState.Leader), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Elect_Anything_On_Conflict_When_Is_Leader()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(
                () => new V1Lease { Spec = new V1LeaseSpec { HolderIdentity = Environment.MachineName, }, });
        _client
            .Setup(c => c.Update(It.IsAny<V1Lease>()))
            .ThrowsAsync(
                new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(
                        new HttpResponseMessage(HttpStatusCode.Conflict),
                        string.Empty),
                });

        await _elector.CheckLeaderLease();

        _election.Verify(e => e.LeadershipChanged(It.IsAny<LeaderState>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Elect_Anything_On_HttpException_When_Is_Leader()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(
                () => new V1Lease { Spec = new V1LeaseSpec { HolderIdentity = Environment.MachineName, }, });
        _client
            .Setup(c => c.Update(It.IsAny<V1Lease>()))
            .ThrowsAsync(
                new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(
                        new HttpResponseMessage(HttpStatusCode.BadGateway),
                        string.Empty),
                });

        await _elector.CheckLeaderLease();

        _election.Verify(e => e.LeadershipChanged(It.IsAny<LeaderState>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Elect_Anything_On_Exception_When_Is_Leader()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(
                () => new V1Lease { Spec = new V1LeaseSpec { HolderIdentity = Environment.MachineName, }, });
        _client
            .Setup(c => c.Update(It.IsAny<V1Lease>()))
            .ThrowsAsync(
                new Exception());

        await _elector.CheckLeaderLease();

        _election.Verify(e => e.LeadershipChanged(It.IsAny<LeaderState>()), Times.Never);
    }

    [Fact]
    public async Task Should_Takeover_Leadership_When_Lease_Ran_Out()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(
                () => new V1Lease
                {
                    Spec = new V1LeaseSpec
                    {
                        HolderIdentity = $"not-{Environment.MachineName}",
                        RenewTime = DateTime.UtcNow.AddMinutes(-1),
                    },
                });

        await _elector.CheckLeaderLease();

        _election.Verify(e => e.LeadershipChanged(LeaderState.Leader), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Elect_Anything_On_Conflict_When_Lease_Ran_Out()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(
                () => new V1Lease
                {
                    Spec = new V1LeaseSpec
                    {
                        HolderIdentity = $"not-{Environment.MachineName}",
                        RenewTime = DateTime.UtcNow.AddMinutes(-1),
                    },
                });
        _client
            .Setup(c => c.Update(It.IsAny<V1Lease>()))
            .ThrowsAsync(
                new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(
                        new HttpResponseMessage(HttpStatusCode.Conflict),
                        string.Empty),
                });

        await _elector.CheckLeaderLease();

        _election.Verify(e => e.LeadershipChanged(It.IsAny<LeaderState>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Elect_Anything_On_HttpException_When_Lease_Ran_Out()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(
                () => new V1Lease
                {
                    Spec = new V1LeaseSpec
                    {
                        HolderIdentity = $"not-{Environment.MachineName}",
                        RenewTime = DateTime.UtcNow.AddMinutes(-1),
                    },
                });
        _client
            .Setup(c => c.Update(It.IsAny<V1Lease>()))
            .ThrowsAsync(
                new HttpOperationException
                {
                    Response = new HttpResponseMessageWrapper(
                        new HttpResponseMessage(HttpStatusCode.BadGateway),
                        string.Empty),
                });

        await _elector.CheckLeaderLease();

        _election.Verify(e => e.LeadershipChanged(It.IsAny<LeaderState>()), Times.Never);
    }

    [Fact]
    public async Task Should_Not_Elect_Anything_On_Exception_When_Lease_Ran_Out()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(
                () => new V1Lease
                {
                    Spec = new V1LeaseSpec
                    {
                        HolderIdentity = $"not-{Environment.MachineName}",
                        RenewTime = DateTime.UtcNow.AddMinutes(-1),
                    },
                });
        _client
            .Setup(c => c.Update(It.IsAny<V1Lease>()))
            .ThrowsAsync(
                new Exception());

        await _elector.CheckLeaderLease();

        _election.Verify(e => e.LeadershipChanged(It.IsAny<LeaderState>()), Times.Never);
    }

    [Fact]
    public async Task Should_Stay_Candidate_When_Lease_Is_Valid()
    {
        _election.Setup(e => e.LeadershipChanged(It.IsAny<LeaderState>()));
        _client
            .Setup(c => c.Get<V1Lease>(It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(
                () => new V1Lease
                {
                    Spec = new V1LeaseSpec
                    {
                        HolderIdentity = $"not-{Environment.MachineName}",
                        RenewTime = DateTime.UtcNow.AddSeconds(-1),
                    },
                });

        await _elector.CheckLeaderLease();

        _election.Verify(e => e.LeadershipChanged(LeaderState.Candidate), Times.Once);
    }
}
