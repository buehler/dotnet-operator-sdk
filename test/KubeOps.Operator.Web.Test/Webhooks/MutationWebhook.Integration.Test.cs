﻿using FluentAssertions;

using KubeOps.KubernetesClient;
using KubeOps.Operator.Web.Test.TestApp;

namespace KubeOps.Operator.Web.Test.Webhooks;

public class MutationWebhookIntegrationTest : IntegrationTestBase
{
    [Fact(Skip = "This test is flakey since localtunnel is not always available. Need an alternative.")]
    public async Task Should_Allow_Creation_Of_Entity()
    {
        using var client = new KubernetesClient.KubernetesClient() as IKubernetesClient;
        var e = await client.CreateAsync(new V1OperatorWebIntegrationTestEntity("test-entity", "foobar"));
        e.Spec.Username.Should().Be("foobar");
        await client.DeleteAsync(e);
    }

    [Fact(Skip = "This test is flakey since localtunnel is not always available. Need an alternative.")]
    public async Task Should_Mutate_Entity_According_To_Code()
    {
        using var client = new KubernetesClient.KubernetesClient() as IKubernetesClient;
        var e = await client.CreateAsync(new V1OperatorWebIntegrationTestEntity("test-entity", "overwrite"));
        e.Spec.Username.Should().Be("overwritten");
        await client.DeleteAsync(e);
    }
}
