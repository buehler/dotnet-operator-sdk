using DotnetKubernetesClient.Entities;
using KubeOps.Operator.Entities.Extensions;
using Xunit;

namespace KubeOps.Test.Operator.Webhook.ConversionWebhook;

public class CustomEntityDefinitionExtensionMethodsTests
{
    [Fact]
    public void ShouldAddNamespaceToName()
    {
        //Arrange
        var customEntityDefinition = new CustomEntityDefinition("test", "tests", "test.group", "v1", "test", "test", EntityScope.Namespaced);

        //Act
        var namespacedName = customEntityDefinition.GroupVersion();
        //Assert
        Assert.Equal("test.group/v1", namespacedName);
    }

    [Fact]
    public void ShouldAddEmptyNamespaceToName()
    {
        //Arrange
        var customEntityDefinition = new CustomEntityDefinition("test", "tests", "", "v1", "test", "test", EntityScope.Namespaced);
        //Act
        var namespacedName = customEntityDefinition.GroupVersion();

        //Assert
        Assert.Equal("/v1", namespacedName);
    }

    [Fact]
    public void ShouldAddNamespaceToEmptyName()
    {
        //Arrange
        var customEntityDefinition = new CustomEntityDefinition("test", "tests", "test.group", "", "test", "test", EntityScope.Namespaced);

        //Act
        var namespacedName = customEntityDefinition.GroupVersion();

        //Assert
        Assert.Equal("test.group/", namespacedName);
    }

}
