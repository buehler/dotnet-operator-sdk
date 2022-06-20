using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotnetKubernetesClient;
using k8s;
using KubeOps.Operator.Webhooks.ConversionWebhook;
using KubeOps.Test.Integration.Operator;

namespace KubeOps.Test.Integration;

public class ConversionWebhookIntegrationTests : IDisposable
{

    private readonly TestOperatorRunner _sut;
    private readonly IKubernetesClient _kubernetesClient;

    public ConversionWebhookIntegrationTests()
    {
        _kubernetesClient =
            new KubernetesClient(KubernetesClientConfiguration.BuildDefaultConfig());
        _sut = new TestOperatorRunner();

        // Used to call EnsureServer()
        var server = _sut.Server;
    }

    public void Dispose()
    {
        _sut.Dispose();
        _kubernetesClient.Delete<V2TestEntity>("testentity","default");
        _kubernetesClient.Delete<V2TestEntity>("testentity1","default");
    }

    [Fact]
    public async Task ConversionWebhookShouldReturnV2TestEntity()
    {
        //Arrange
        using var client = _sut.CreateClient();
        var guid = Guid.NewGuid();
        var request = new Request(guid, "integration.testing.dev/v2", new List<object>() { new V1TestEntity(){Kind = "TestEntity", ApiVersion = "integration.testing.dev/v1",} });
        var conversionReview = new ConversionReview(
            "integration.testing.dev/v1",
            "ConversionReview",
            request);
        var content = JsonContent.Create(conversionReview, typeof(ConversionReview));
        //Act
        var responseMessage = await client.PostAsync("/convert", content);
        Assert.True(responseMessage.IsSuccessStatusCode);
        var jsonContent = await responseMessage.Content.ReadAsStringAsync();
        var responseContent = JsonSerializer.Deserialize<ConversionResponse<V2TestEntity>>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        //Assert
        Assert.NotNull(responseContent);
        Assert.Equal(guid,responseContent.Response.Uid);
        Assert.Equal("Success",responseContent.Response.Result.Status);
        Assert.Single(responseContent.Response.ConvertedObjects!);
        Assert.Equal("integration.testing.dev/v2",responseContent.Response.ConvertedObjects!.First().ApiVersion);
    }

    [Fact]
    public async Task ConversionWebhookShouldReturnBadRequestIfNoJSON()
    {
        //Arrange
        using var client = _sut.CreateClient();
        //Act
        var responseMessage = await client.PostAsync("/convert", null);
        //Assert
        Assert.Equal(HttpStatusCode.BadRequest, responseMessage.StatusCode);
    }

    [Fact]
    public async Task ConversionWebhookShouldReturnResultFailedWhenExceptionThrown()
    {
        //Arrange
        using var client = _sut.CreateClient();
        var guid = Guid.NewGuid();
        var request = new Request(guid, "integration.testing.dev/v2", new List<object>() { new V1TestEntity()
        {
            Kind = "TestEntity", ApiVersion = "integration.testing.dev/v1",Spec = new V1TestEntitySpec(){Spec = "throwException"},
        }});
        var conversionReview = new ConversionReview(
            "integration.testing.dev/v1",
            "ConversionReview",
            request);
        var content = JsonContent.Create(conversionReview, typeof(ConversionReview));
        //Act
        var responseMessage = await client.PostAsync("/convert", content);
        Assert.True(responseMessage.IsSuccessStatusCode);
        var responseContent = await JsonSerializer.DeserializeAsync<ConversionResponse<V2TestEntity>>(await responseMessage.Content.ReadAsStreamAsync(),new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        //Assert
        Assert.NotNull(responseContent);
        Assert.Equal("Failed",responseContent.Response.Result.Status);
    }
}
