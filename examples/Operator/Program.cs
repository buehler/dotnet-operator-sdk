using KubeOps.Operator.Extensions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Operator.Controller;
using Operator.Entities;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Trace);

builder.Services
    .AddKubernetesOperator()
    .AddController<V1TestEntityController, V1TestEntity>(new("TestEntity", "v1", "testing.dev"));

using var host = builder.Build();
await host.RunAsync();
