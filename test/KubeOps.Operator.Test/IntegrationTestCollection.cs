using System.Reflection;

using k8s;
using k8s.Models;

using KubeOps.Operator.Test.TestEntities;
using KubeOps.Transpiler;

namespace KubeOps.Operator.Test;

[CollectionDefinition(Name, DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<CrdInstaller>, ICollectionFixture<MlcProvider>
{
    public const string Name = "Integration Tests";
}

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IClassFixture<HostBuilder>
{
    protected readonly HostBuilder _hostBuilder;
    protected readonly MetadataLoadContext _mlc;

    protected IntegrationTestBase(HostBuilder hostBuilder, MlcProvider provider)
    {
        _hostBuilder = hostBuilder;
        _mlc = provider.Mlc;
    }
}

public sealed class CrdInstaller : IAsyncLifetime
{
    private List<V1CustomResourceDefinition> _crds = new();

    public async Task InitializeAsync()
    {
        await using var p = new MlcProvider();
        await p.InitializeAsync();
        _crds = p.Mlc.Transpile(new[] { typeof(V1IntegrationTestEntity) }).ToList();

        using var client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        foreach (var crd in _crds)
        {
            switch (await client.ApiextensionsV1.ListCustomResourceDefinitionAsync(
                        fieldSelector: $"metadata.name={crd.Name()}"))
            {
                case { Items: [var existing] }:
                    crd.Metadata.ResourceVersion = existing.ResourceVersion();
                    await client.ApiextensionsV1.ReplaceCustomResourceDefinitionAsync(crd, crd.Name());
                    break;
                default:
                    await client.ApiextensionsV1.CreateCustomResourceDefinitionAsync(crd);
                    break;
            }
        }
    }

    public async Task DisposeAsync()
    {
        using var client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        foreach (var crd in _crds)
        {
            await client.ApiextensionsV1.DeleteCustomResourceDefinitionAsync(crd.Name());
        }
    }
}
