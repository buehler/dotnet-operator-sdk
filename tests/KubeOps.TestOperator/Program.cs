using System.Threading.Tasks;
using KubeOps.Operator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace KubeOps.TestOperator
{
    public static class Program
    {
        public static Task<int> Main(string[] args) => CreateHostBuilder(args).Build().RunOperatorAsync(args);

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}
