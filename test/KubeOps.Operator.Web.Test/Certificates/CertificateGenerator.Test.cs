using System.Security.Cryptography.X509Certificates;

using FluentAssertions;

using KubeOps.Operator.Web.Certificates;

namespace KubeOps.Operator.Web.Test.Certificates;

public class CertificateGeneratorTest : IDisposable
{
    private readonly CertificateGenerator _certificateGenerator = new(Environment.MachineName);

    [Fact]
    public void Root_Should_Be_Valid()
    {
        var (certificate, key) = _certificateGenerator.Root;

        certificate.Should().NotBeNull();
        DateTime.Parse(certificate.GetEffectiveDateString()).Should().BeOnOrBefore(DateTime.UtcNow);
        certificate.Extensions.Any(e => e is X509BasicConstraintsExtension basic && basic.CertificateAuthority).Should().BeTrue();
        certificate.HasPrivateKey.Should().BeTrue();

        key.Should().NotBeNull();
    }

    [Fact]
    public void Server_Should_Be_Valid()
    {
        var (certificate, key) = _certificateGenerator.Server;

        certificate.Should().NotBeNull();
        DateTime.Parse(certificate.GetEffectiveDateString()).Should().BeOnOrBefore(DateTime.UtcNow);
        certificate.Extensions.Any(e => e is X509BasicConstraintsExtension basic && basic.CertificateAuthority).Should().BeFalse();
        certificate.HasPrivateKey.Should().BeFalse();

        key.Should().NotBeNull();
    }

    public void Dispose()
    {
        _certificateGenerator.Dispose();
        GC.SuppressFinalize(this);
    }
}
