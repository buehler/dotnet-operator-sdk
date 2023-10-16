using FluentAssertions;

using k8s.Autorest;

using KubeOps.Operator.Client;
using KubeOps.Operator.Web.Test.TestApp;

namespace KubeOps.Operator.Web.Test.Webhooks;

public class ValidationWebhookIntegrationTest : IntegrationTestBase
{
    [Fact(Skip = "This test is flakey since localtunnel is not always available. Need an alternative.")]
    public async Task Should_Allow_Creation_Of_Entity()
    {
        using var client = KubernetesClientFactory.Create<V1OperatorWebIntegrationTestEntity>();
        var e = await client.CreateAsync(new V1OperatorWebIntegrationTestEntity("test-entity", "foobar"));
        await client.DeleteAsync(e);
    }

    [Fact(Skip = "This test is flakey since localtunnel is not always available. Need an alternative.")]
    public async Task Should_Disallow_Creation_When_Validation_Fails()
    {
        using var client = KubernetesClientFactory.Create<V1OperatorWebIntegrationTestEntity>();
        var ex = await Assert.ThrowsAsync<HttpOperationException>(async () => await client.CreateAsync(new V1OperatorWebIntegrationTestEntity("test-entity", "forbidden")));
        ex.Message.Should().Contain("name may not be 'forbidden'");
    }
}
