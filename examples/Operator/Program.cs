using KubeOps.Operator.Extensions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Operator.Controller;
using Operator.Entities;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Trace);

builder.Services
    .AddKubernetesOperator()
    .RegisterEntitiyMetadata()
    .AddController<V1TestEntityController, V1TestEntity>();

using var host = builder.Build();
await host.RunAsync();
