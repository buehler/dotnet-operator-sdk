using KubeOps.Operator;
using KubeOps.TestOperator.TestManager;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KubeOps.TestOperator.Test;

public class TestAssemblyScannedStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddKubernetesOperator(s => { s.Name = "test-operator"; })
            .AddResourceAssembly(typeof(Program).Assembly);

        services.AddSingleton<Mock<IManager>>();
        services.AddSingleton(provider => provider.GetRequiredService<Mock<IManager>>().Object);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseKubernetesOperator();
    }
}
