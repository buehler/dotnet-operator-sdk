using FluentAssertions;

using KubeOps.Abstractions.Builder;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Web.Builder;
using KubeOps.Operator.Web.LocalTunnel;
using KubeOps.Operator.Web.Webhooks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Web.Test.Builder;

public class OperatorBuilderExtensionsTest
{
    private readonly IOperatorBuilder _builder = new OperatorBuilder(new ServiceCollection(), new());

    [Fact]
    public void Should_Add_Development_Tunnel()
    {
        _builder.AddDevelopmentTunnel(4242);

        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(IHostedService) &&
            s.ImplementationType == typeof(TunnelWebhookService) &&
            s.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void Should_Add_TunnelConfig()
    {
        _builder.AddDevelopmentTunnel(1337, "my-host");

        _builder.Services.Should().Contain(s =>
            s.ServiceType == typeof(WebhookConfig) &&
            s.Lifetime == ServiceLifetime.Singleton);
    }
}
