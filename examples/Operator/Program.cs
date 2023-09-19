using KubeOps.Operator.Extensions;

using Microsoft.Extensions.Hosting;

using Operator.Controller;
using Operator.Entities;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddKubernetesOperator()
    .AddController<V1TestEntityController, V1TestEntity>();

using var host = builder.Build();
await host.RunAsync();
