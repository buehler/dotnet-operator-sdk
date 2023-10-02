using KubeOps.Operator.Extensions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Operator.Entities;
using Operator.Finalizer;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Trace);

builder.Services
    .AddKubernetesOperator()
    .RegisterResources()
    .AddFinalizer<FinalizerOne, V1TestEntity>("finalizer.one")
    .AddFinalizer<FinalizerTwo, V1TestEntity>("finalizer.two");

using var host = builder.Build();
await host.RunAsync();
