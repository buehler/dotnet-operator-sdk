using KubeOps.Operator.Extensions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Trace);

builder.Services
    .AddKubernetesOperator()
    .RegisterResources();

using var host = builder.Build();
await host.RunAsync();
