using KubeOps.Operator;
using KubeOps.TestOperator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });

await CreateHostBuilder(args).Build().RunOperatorAsync(args);
