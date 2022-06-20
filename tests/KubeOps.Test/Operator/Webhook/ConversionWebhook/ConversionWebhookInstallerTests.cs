using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using KubeOps.Operator.Webhooks;
using KubeOps.Operator.Webhooks.ConversionWebhook;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KubeOps.Enhancements.Test;

public class ConversionWebhookInstallerTests
{
    private readonly ConversionWebhookInstaller _conversionWebhookInstaller;
    private readonly Mock<IConversionWebhookBuilder> _conversionWebhookBuilder;
    private readonly Mock<IKubernetesClient> _kubernetesClient;
    private readonly Mock<ILogger<ConversionWebhookInstaller>> _logger;

    public ConversionWebhookInstallerTests()
    {
        _logger = new Mock<ILogger<ConversionWebhookInstaller>>();
        _conversionWebhookBuilder = new Mock<IConversionWebhookBuilder>();
        _kubernetesClient = new Mock<IKubernetesClient>();
        _conversionWebhookInstaller = new ConversionWebhookInstaller(
            _conversionWebhookBuilder.Object,
            _kubernetesClient.Object,
            _logger.Object);
    }

    [Fact]
    public async Task InstallShouldNotCallClientIfNoCustomResourceDefenitionsReturned()
    {
        //Arrange
        var config = new WebhookConfig("test", null, null, null);
        _conversionWebhookBuilder.Setup(b => b.BuildWebhookConfiguration(config, false))
            .Returns(new List<V1CustomResourceDefinition>());
        //Act
        await _conversionWebhookInstaller.InstallConversionWebhooks(config);
        //Assert
        _kubernetesClient.Verify(c=>c.Save(It.IsAny<IKubernetesObject<V1ObjectMeta>>()), Times.Never);
    }

    [Fact]
    public async Task InstallShouldCallClientOnceIfOneCrdIsFound()
    {
        //Arrange
        var config = new WebhookConfig("test", null, null, null);
        _conversionWebhookBuilder.Setup(b => b.BuildWebhookConfiguration(config, false))
            .Returns(new List<V1CustomResourceDefinition>(){new(){Spec = new V1CustomResourceDefinitionSpec(){Names = new V1CustomResourceDefinitionNames("test","test")}}});
        //Act
        await _conversionWebhookInstaller.InstallConversionWebhooks(config);
        //Assert
        _kubernetesClient.Verify(c=>c.Save(It.IsAny<IKubernetesObject<V1ObjectMeta>>()), Times.Once);
    }

    [Fact]
    public async Task InstallShouldCallClientTwiceIfTwoCrdAreFound()
    {
        //Arrange
        var config = new WebhookConfig("test", null, null, null);
        _conversionWebhookBuilder.Setup(b => b.BuildWebhookConfiguration(config, false))
            .Returns(new List<V1CustomResourceDefinition>(){new(){Spec = new V1CustomResourceDefinitionSpec(){Names = new V1CustomResourceDefinitionNames("test","test")}}, new(){Spec = new V1CustomResourceDefinitionSpec(){Names = new V1CustomResourceDefinitionNames("test","test")}}});
        //Act
        await _conversionWebhookInstaller.InstallConversionWebhooks(config);
        //Assert
        _kubernetesClient.Verify(c=>c.Save(It.IsAny<IKubernetesObject<V1ObjectMeta>>()), Times.Exactly(2));
    }


}
