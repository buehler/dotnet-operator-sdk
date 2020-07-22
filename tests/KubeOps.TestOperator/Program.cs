using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace KubeOps.TestOperator
{
    public static class Program
    {
        public static Task<int> Main(string[] args) => new Operator()
            .ConfigureWebHost(host => host.UseStartup<Startup>())
            .Run(args);
    }

    public class Startup
    {
        public void Configure(IApplicationBuilder builder)
        {
            builder.UseRouting();
            builder.UseEndpoints(e => e.MapGet("status", async ctx =>
            {
                await ctx.Response.WriteAsync("hello world");
            }));
        }
    }
}
