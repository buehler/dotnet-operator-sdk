using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using k8s.Models;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Webhooks;
using KubeOps.Operator.Webhooks.ConversionWebhook;
using KubeOps.Test.TestEntities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KubeOps.Test.Operator.Webhook.ConversionWebhook;

public class ConversionWebhookBuilderTests
{
    private readonly ConversionWebhookBuilder _conversionWebhookBuilder;
    private readonly Mock<ICrdBuilder> _crdBuilder;
    private readonly Mock<IComponentRegistrar> _componentRegistrar;

    public ConversionWebhookBuilderTests()
    {
        _crdBuilder = new Mock<ICrdBuilder>();
        _componentRegistrar = new Mock<IComponentRegistrar>();
        _conversionWebhookBuilder = new ConversionWebhookBuilder(_crdBuilder.Object, _componentRegistrar.Object,Mock.Of<ILogger<ConversionWebhookBuilder>>());
    }

    [Fact]
    public void ConversionWebhookBuilderShouldSetConfigForLocalTunnel()
    {
        //Arrange
        _componentRegistrar.Setup(c => c.ConversionRegistrations)
            .Returns(ImmutableHashSet.Create(new IComponentRegistrar.ConversionRegistration(typeof(TestConversionWebhook),typeof(ConversionTestEntityV1Beta),typeof(ConversionTestEntityV1))));
        _crdBuilder.Setup(s => s.BuildCrds()).Returns(new List<V1CustomResourceDefinition>()
        {
            new (){
                Spec = new V1CustomResourceDefinitionSpec(){
                    Group = "kubeops.test.dev",
                    Versions = new List<V1CustomResourceDefinitionVersion>()
            {
                new ("v1beta1", true, false),
                new ("v1", true, true),
            }}},
        });
        WebhookConfig config = new("test", "https://localhost:5001/", null, null);
        //Act
        var result = _conversionWebhookBuilder.BuildWebhookConfiguration(config, true).ToList();
        //Assert
        Assert.Equal("Webhook",result.First().Spec.Conversion.Strategy);
        Assert.Equal("https://localhost:5001/convert",result.First().Spec.Conversion.Webhook.ClientConfig.Url);
        Assert.Equal("v1",result.First().Spec.Conversion.Webhook.ConversionReviewVersions.First());
    }

    [Fact]
    public void ConversionWebhookBuilderShouldReturnEmptyList()
    {
        //Arrange
        _componentRegistrar.Setup(c => c.ConversionRegistrations)
            .Returns(ImmutableHashSet<IComponentRegistrar.ConversionRegistration>.Empty);
        _crdBuilder.Setup(s => s.BuildCrds()).Returns(new List<V1CustomResourceDefinition>()
        {
            new (){
                Spec = new V1CustomResourceDefinitionSpec(){
                    Group = "kubeops.test1.dev",
                    Versions = new List<V1CustomResourceDefinitionVersion>()
                    {
                        new ("v1beta1", true, false),
                        new ("v1", true, true),
                    }}},
        });
        WebhookConfig config = new("test", "https://localhost:5001/", null, null);
        //Act
        var result = _conversionWebhookBuilder.BuildWebhookConfiguration(config, true).ToList();
        //Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ConversionWebhookBuilderShouldSetConfig()
    {
        //Arrange
        _componentRegistrar.Setup(c => c.ConversionRegistrations)
            .Returns(ImmutableHashSet.Create(new IComponentRegistrar.ConversionRegistration(typeof(TestConversionWebhook),typeof(ConversionTestEntityV1Beta),typeof(ConversionTestEntityV1))));
        _crdBuilder.Setup(s => s.BuildCrds()).Returns(new List<V1CustomResourceDefinition>()
        {
            new (){
                Spec = new V1CustomResourceDefinitionSpec(){
                    Group = "kubeops.test.dev",
                    Versions = new List<V1CustomResourceDefinitionVersion>()
                    {
                        new ("v1beta1", true, false),
                        new ("v1", true, true),
                    }}},
        });
        var bundle = new[] { byte.MinValue };
        WebhookConfig config = new("test", null, bundle, new Admissionregistrationv1ServiceReference("testService", "default", "/convert", 5000));
        //Act
        var result = _conversionWebhookBuilder.BuildWebhookConfiguration(config, false).ToList();
        //Assert
        Assert.Equal("Webhook",result.First().Spec.Conversion.Strategy);
        Assert.Null(result.First().Spec.Conversion.Webhook.ClientConfig.Url);
        Assert.Equal("v1",result.First().Spec.Conversion.Webhook.ConversionReviewVersions.First());
        Assert.Equal(5000,result.First().Spec.Conversion.Webhook.ClientConfig.Service.Port);
        Assert.Equal(bundle,result.First().Spec.Conversion.Webhook.ClientConfig.CaBundle);
        Assert.Equal("/convert",result.First().Spec.Conversion.Webhook.ClientConfig.Service.Path);
        Assert.Equal("default",result.First().Spec.Conversion.Webhook.ClientConfig.Service.NamespaceProperty);
    }
}
