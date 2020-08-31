using KubeOps.Operator;
using KubeOps.TestOperator.Controller;
using KubeOps.TestOperator.Finalizer;
using KubeOps.TestOperator.TestManager;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.TestOperator
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddKubernetesOperator(s => s.Name = "test-operator")
                .AddFinalizer<TestEntityFinalizer>()
                .AddController<TestController>();

            services.AddTransient<IManager, TestManager.TestManager>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseKubernetesOperator();
        }
    }
}
