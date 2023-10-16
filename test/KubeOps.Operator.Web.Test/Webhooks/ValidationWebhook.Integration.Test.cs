using KubeOps.Operator.Client;
using KubeOps.Operator.Web.Test.TestApp;

namespace KubeOps.Operator.Web.Test.Webhooks;

public class ValidationWebhook : IntegrationTestBase
{
    public ValidationWebhook(TestApplicationFactory factory) : base(factory)
    {
    }

    [Fact(Skip = "I have no idea why Kubernetes cannot connect to the local tunnel.")]
    public async Task Should_Allow_Creation_Of_Entity()
    {
        using var client = KubernetesClientFactory.Create<V1IntegrationTestEntity>();
        await client.CreateAsync(new V1IntegrationTestEntity("test-entity", "foobar"));
    }
}
