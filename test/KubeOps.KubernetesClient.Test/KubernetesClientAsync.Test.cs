using FluentAssertions;

using k8s.Models;

namespace KubeOps.KubernetesClient.Test;

public class KubernetesClientAsyncTest : IntegrationTestBase, IDisposable
{
    private readonly IKubernetesClient _client =
        new KubernetesClient();

    private readonly IList<V1ConfigMap> _objects = new List<V1ConfigMap>();

    [Fact]
    public async Task Should_Return_Namespace()
    {
        var ns = await _client.GetCurrentNamespaceAsync();
        ns.Should().Be("default");
    }

    [Fact]
    public async Task Should_Create_Some_Object()
    {
        var config = await _client.CreateAsync(
            new V1ConfigMap
            {
                Kind = V1ConfigMap.KubeKind,
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Metadata = new(name: RandomName(), namespaceProperty: "default"),
                Data = new Dictionary<string, string> { { "Hello", "World" } },
            });

        _objects.Add(config);

        config.Metadata.Should().NotBeNull();
        config.Metadata.ResourceVersion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_Get_Some_Object()
    {
        var config = await _client.CreateAsync(
            new V1ConfigMap
            {
                Kind = V1ConfigMap.KubeKind,
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Metadata = new(name: RandomName(), namespaceProperty: "default"),
                Data = new Dictionary<string, string> { { "Hello", "World" } },
            });

        _objects.Add(config);
        _objects.Add(await _client.CreateAsync(
            new V1ConfigMap
            {
                Kind = V1ConfigMap.KubeKind,
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Metadata = new(name: RandomName(), namespaceProperty: "default"),
                Data = new Dictionary<string, string> { { "Hello", "World" } },
            }));
        _objects.Add(await _client.CreateAsync(
            new V1ConfigMap
            {
                Kind = V1ConfigMap.KubeKind,
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Metadata = new(name: RandomName(), namespaceProperty: "default"),
                Data = new Dictionary<string, string> { { "Hello", "World" } },
            }));

        var fetched = await _client.GetAsync<V1ConfigMap>(config.Name(), config.Namespace());
        fetched!.Name().Should().Be(config.Name());
    }

    [Fact]
    public async Task Should_Update_Some_Object()
    {
        var config = await _client.CreateAsync(
            new V1ConfigMap
            {
                Kind = V1ConfigMap.KubeKind,
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Metadata = new V1ObjectMeta(name: RandomName(), namespaceProperty: "default"),
                Data = new Dictionary<string, string> { { "Hello", "World" } },
            });
        var r1 = config.Metadata.ResourceVersion;
        _objects.Add(config);

        config.Data.Add("test", "value");
        config = await _client.UpdateAsync(config);
        var r2 = config.Metadata.ResourceVersion;

        r1.Should().NotBe(r2);
    }

    [Fact]
    public async Task Should_List_Some_Objects()
    {
        var config1 = await _client.CreateAsync(
            new V1ConfigMap
            {
                Kind = V1ConfigMap.KubeKind,
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Metadata = new(name: RandomName(), namespaceProperty: "default"),
                Data = new Dictionary<string, string> { { "Hello", "World" } },
            });
        var config2 = await _client.CreateAsync(
            new V1ConfigMap
            {
                Kind = V1ConfigMap.KubeKind,
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Metadata = new(name: RandomName(), namespaceProperty: "default"),
                Data = new Dictionary<string, string> { { "Hello", "World" } },
            });

        _objects.Add(config1);
        _objects.Add(config2);

        var configs = await _client.ListAsync<V1ConfigMap>("default");

        // there are _at least_ 2 config maps (the two that were created)
        configs.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Should_Delete_Some_Object()
    {
        var config1 = await _client.CreateAsync(
            new V1ConfigMap
            {
                Kind = V1ConfigMap.KubeKind,
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Metadata = new(name: RandomName(), namespaceProperty: "default"),
                Data = new Dictionary<string, string> { { "Hello", "World" } },
            });
        var config2 = await _client.CreateAsync(
            new V1ConfigMap
            {
                Kind = V1ConfigMap.KubeKind,
                ApiVersion = V1ConfigMap.KubeApiVersion,
                Metadata = new(name: RandomName(), namespaceProperty: "default"),
                Data = new Dictionary<string, string> { { "Hello", "World" } },
            });
        _objects.Add(config1);

        var configs = await _client.ListAsync<V1ConfigMap>("default");
        configs.Count.Should().BeGreaterThanOrEqualTo(2);

        await _client.DeleteAsync(config2);

        configs = await _client.ListAsync<V1ConfigMap>("default");
        configs.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Should_Not_Throw_On_Not_Found_Delete()
    {
        var config = new V1ConfigMap
        {
            Kind = V1ConfigMap.KubeKind,
            ApiVersion = V1ConfigMap.KubeApiVersion,
            Metadata = new(name: RandomName(), namespaceProperty: "default"),
            Data = new Dictionary<string, string> { { "Hello", "World" } },
        };
        await _client.DeleteAsync(config);
    }

    public void Dispose()
    {
        _client.Delete(_objects);
    }

    private static string RandomName() => "cm-" + Guid.NewGuid().ToString().ToLower();
}
