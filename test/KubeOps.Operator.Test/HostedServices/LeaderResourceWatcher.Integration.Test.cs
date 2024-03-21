using KubeOps.Abstractions.Controller;
using KubeOps.Operator.Test.TestEntities;

using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Test.HostedServices;

public class LeaderAwareHostedServiceDisposeIntegrationTest : HostedServiceDisposeIntegrationTest
{
    protected override void ConfigureHost(HostApplicationBuilder builder)
    {
        builder.Services
            .AddKubernetesOperator(op => op.EnableLeaderElection = true)
            .AddController<TestController, V1OperatorIntegrationTestEntity>();
    }

    private class TestController : IEntityController<V1OperatorIntegrationTestEntity>
    {
        public Task ReconcileAsync(V1OperatorIntegrationTestEntity entity, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task DeletedAsync(V1OperatorIntegrationTestEntity entity, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
