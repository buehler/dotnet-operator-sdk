using k8s;

using KubeOps.Cli;

using McMaster.Extensions.CommandLineUtils;

using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddSingleton<IKubernetes>(new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig()))
    .BuildServiceProvider();

var app = new CommandLineApplication<Entrypoint>();
app.Conventions
    .UseDefaultConventions()
    .UseConstructorInjection(services);

await app.ExecuteAsync(args);
