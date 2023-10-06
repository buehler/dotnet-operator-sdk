using k8s.Models;

using KubeOps.Operator;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Trace);

builder.Services
    .AddKubernetesOperator()
    .RegisterComponents()
    .AddEntity<V1ConfigMap>(
        new(
            V1ConfigMap.KubeKind,
            V1ConfigMap.KubeApiVersion,
            V1ConfigMap.KubeGroup,
            V1ConfigMap.KubePluralName));

using var host = builder.Build();
await host.RunAsync();
