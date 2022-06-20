using KubeOps.Operator;
using Xunit;

namespace KubeOps.Test.Operator;

public class OperatorSettingsTests
{
    [Fact]
    public void UseLocalTunnelShouldSetLocalTunnelPropertyInOperatorSettingsStandardValues()
    {
        //Arrange
        var settings = new OperatorSettings();
        //Act
        settings.UseLocalTunnel();
        //Assert
        Assert.Equal("localhost", settings.LocalTunnelSettings.Host);
        Assert.Equal(5000,settings.LocalTunnelSettings.Port);
        Assert.False(settings.LocalTunnelSettings.IsHttps);
        Assert.True(settings.LocalTunnelSettings.AllowUntrustedCertificates);
        Assert.True(settings.LocalTunnelSettings.UseLocalTunnel);
    }

    [Fact]
    public void UseLocalTunnelShouldSetLocalTunnelPropertyInOperatorSettingsNonStandardValues()
    {
        //Arrange
        var settings = new OperatorSettings();
        //Act
        settings.UseLocalTunnel(false, true, "testHost", 80);
        //Assert
        Assert.Equal("testHost", settings.LocalTunnelSettings.Host);
        Assert.Equal(80,settings.LocalTunnelSettings.Port);
        Assert.True(settings.LocalTunnelSettings.IsHttps);
        Assert.False(settings.LocalTunnelSettings.AllowUntrustedCertificates);
        Assert.True(settings.LocalTunnelSettings.UseLocalTunnel);
    }

}
