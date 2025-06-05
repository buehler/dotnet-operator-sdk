using KubeOps.Operator;

using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddKubernetesOperator()
    .RegisterComponents();

using var host = builder.Build();
await host.RunAsync();
