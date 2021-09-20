using KubeOps.Operator;
using KubeOps.TestOperator.TestManager;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.TestOperator
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddKubernetesOperator(s => s.EnableLeaderElection = false);//.AddWebhookLocaltunnel();
            services.AddTransient<IManager, TestManager.TestManager>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseKubernetesOperator();
        }
    }
}
