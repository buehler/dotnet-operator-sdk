using FluentAssertions;

using KubeOps.Operator.Client;
using KubeOps.Operator.Web.Test.TestApp;

namespace KubeOps.Operator.Web.Test.Webhooks;

public class MutationWebhookIntegrationTest : IntegrationTestBase
{
    [Fact]
    public async Task Should_Allow_Creation_Of_Entity()
    {
        using var client = KubernetesClientFactory.Create<V1OperatorWebIntegrationTestEntity>();
        var e = await client.CreateAsync(new V1OperatorWebIntegrationTestEntity("test-entity", "foobar"));
        e.Spec.Username.Should().Be("foobar");
        await client.DeleteAsync(e);
    }

    [Fact]
    public async Task Should_Mutate_Entity_According_To_Code()
    {
        using var client = KubernetesClientFactory.Create<V1OperatorWebIntegrationTestEntity>();
        var e = await client.CreateAsync(new V1OperatorWebIntegrationTestEntity("test-entity", "overwrite"));
        e.Spec.Username.Should().Be("overwritten");
        await client.DeleteAsync(e);
    }
}
