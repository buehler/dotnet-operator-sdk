using KubeOps.Operator;
using KubeOps.TestOperator.Controller;
using KubeOps.TestOperator.Finalizer;
using KubeOps.TestOperator.TestManager;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace KubeOps.TestOperator.Test
{
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddKubernetesOperator(s => s.Name = "test-operator")
                .AddFinalizer<TestEntityFinalizer>()
                .AddController<TestController>();

            services.AddSingleton(new Mock<IManager>());
            services.AddSingleton(typeof(IManager), provider => provider.GetRequiredService<Mock<IManager>>().Object);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseKubernetesOperator();
        }
    }
}
