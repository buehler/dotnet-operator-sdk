using k8s;

using KubeOps.Cli.Commands;
using KubeOps.Cli.Output;

using McMaster.Extensions.CommandLineUtils;

using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddSingleton<IKubernetes>(new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig()))
    .AddSingleton<ConsoleOutput>()
    .AddSingleton<ResultOutput>()
    .AddSingleton(PhysicalConsole.Singleton)
    .BuildServiceProvider();

var app = new CommandLineApplication<Entrypoint>();
app.Conventions
    .UseDefaultConventions()
    .UseConstructorInjection(services);

await app.ExecuteAsync(args);
