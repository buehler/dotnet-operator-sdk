using KubeOps.Operator;
using KubeOps.TestOperator.Controller;
using KubeOps.TestOperator.Finalizer;
using KubeOps.TestOperator.TestManager;
using KubeOps.TestOperator.Webhooks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KubeOps.TestOperator.Test;

public class TestStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddKubernetesOperator(
                s =>
                {
                    s.Name = "test-operator";
                    s.EnableAssemblyScanning = false;
                })
            .AddController<TestController>()
            .AddFinalizer<TestEntityFinalizer>()
            .AddValidationWebhook<TestValidator>()
            .AddMutationWebhook<TestMutator>();

        services.AddSingleton<Mock<IManager>>();
        services.AddSingleton(provider => provider.GetRequiredService<Mock<IManager>>().Object);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseKubernetesOperator();
    }
}
