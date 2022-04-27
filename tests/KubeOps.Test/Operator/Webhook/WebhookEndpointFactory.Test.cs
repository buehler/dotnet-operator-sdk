using FluentAssertions;
using k8s.Models;
using KubeOps.Operator.Webhooks;
using Xunit;

namespace KubeOps.Test.Operator.Webhook
{
    public class WebhookEndpointFactoryTest
    {
        [Fact]
        public void When_group_is_empty_then_WebhookEndpointFactory_should_not_create_empty_path_segments()
        {
            var result = WebhookEndpointFactory.Create<V1Pod>(typeof(WebhookEndpointFactoryTest), "/mutator");
            result.Should().Be("/v1/pods/webhookendpointfactorytest/mutator");
        }
    }
}
