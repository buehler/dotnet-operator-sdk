using k8s;
using k8s.Models;

using KubeOps.Operator.Test.TestEntities;
using KubeOps.Transpiler;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Test;

[CollectionDefinition(Name, DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<CrdInstaller>
{
    public const string Name = "Integration Tests";
}

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IClassFixture<HostBuilder>
{
    protected readonly HostBuilder _hostBuilder;

    protected IntegrationTestBase(HostBuilder hostBuilder)
    {
        _hostBuilder = hostBuilder;
    }
}

public sealed class HostBuilder : IAsyncDisposable
{
    private IHost? _host;
    private bool _isRunning;

    public async Task ConfigureAndStart(Action<HostApplicationBuilder> configure)
    {
        if (_host is not null && _isRunning)
        {
            return;
        }

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        configure(builder);
        _host = builder.Build();
        await _host.StartAsync();
        _isRunning = true;
    }

    public async ValueTask DisposeAsync()
    {
        _isRunning = false;
        if (_host is null)
        {
            return;
        }

        await _host.StopAsync();
        _host.Dispose();
    }
}

public sealed class CrdInstaller : IDisposable
{
    private readonly IReadOnlyList<V1CustomResourceDefinition> _crds = Crds
        .Transpile(new[] { typeof(V1IntegrationTestEntity) })
        .ToList();

    public CrdInstaller()
    {
        using var client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        foreach (var crd in _crds)
        {
            switch (client.ApiextensionsV1.ListCustomResourceDefinition(fieldSelector: $"metadata.name={crd.Name()}"))
            {
                case { Items: [var existing] }:
                    crd.Metadata.ResourceVersion = existing.ResourceVersion();
                    client.ApiextensionsV1.ReplaceCustomResourceDefinition(crd, crd.Name());
                    break;
                default:
                    client.ApiextensionsV1.CreateCustomResourceDefinition(crd);
                    break;
            }
        }
    }

    public void Dispose()
    {
        using var client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        foreach (var crd in _crds)
        {
            client.ApiextensionsV1.DeleteCustomResourceDefinition(crd.Name());
        }
    }
}
