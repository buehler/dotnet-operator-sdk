using KubeOps.Operator;
using KubeOps.TestOperator.Controller;
using KubeOps.TestOperator.Entities;
using KubeOps.TestOperator.Finalizer;
using KubeOps.TestOperator.TestManager;
using KubeOps.TestOperator.Webhooks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.TestOperator
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddKubernetesOperator();
            services.AddTransient<IManager, TestManager.TestManager>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseKubernetesOperator();
        }
    }
}
