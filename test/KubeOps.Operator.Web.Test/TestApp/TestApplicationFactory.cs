using KubeOps.Operator.Web.Builder;
using KubeOps.Operator.Web.LocalTunnel;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Web.Test.TestApp;

public class TestApplicationFactory : WebApplicationFactory<TestApplicationFactory.TestStartup>
{
    protected override IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(w => w.UseStartup<TestStartup>().ConfigureKestrel(o => o.ListenAnyIP(5000)));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureKestrel(c => c.ListenAnyIP(5000));
        builder.UseSolutionRelativeContentRoot("test/KubeOps.Operator.Web.Test");
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.BaseAddress = new("http://localhost:5000");
    }

    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(c =>
            {
                c.AddSimpleConsole();
#if DEBUG
                c.SetMinimumLevel(LogLevel.Trace);
#else
                c.SetMinimumLevel(LogLevel.None);
#endif
            });
            services.AddControllers();
            services
                .AddKubernetesOperator()
                .AddDevelopmentTunnel(5000);
            services.RemoveAll<WebhookLoader>();
            services.AddSingleton(new WebhookLoader(typeof(TestApplicationFactory).Assembly));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseDeveloperExceptionPage();
            app.UseEndpoints(b => b.MapControllers());
        }
    }
}
