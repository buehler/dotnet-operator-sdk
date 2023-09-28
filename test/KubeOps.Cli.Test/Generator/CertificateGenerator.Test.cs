using System.CommandLine;
using System.CommandLine.Invocation;

using FluentAssertions;

using KubeOps.Cli.Commands.Generator;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

using Spectre.Console.Testing;

namespace KubeOps.Cli.Test.Generator;

public class CertificateGeneratorTest
{
    [Fact]
    public async Task Should_Execute()
    {
        var console = new TestConsole();

        var cmd = CertificateGenerator.Command;
        var ctx = new InvocationContext(
            cmd.Parse("server", "namespace"));

        await CertificateGenerator.Handler(console, ctx);

        ctx.ExitCode.Should().Be(ExitCodes.Success);
    }

    [Theory]
    [InlineData("ca.pem")]
    [InlineData("ca-key.pem")]
    [InlineData("svc.pem")]
    [InlineData("svc-key.pem")]
    public async Task Should_Generate_Certificate_Files(string file)
    {
        var console = new TestConsole();

        var cmd = CertificateGenerator.Command;
        var ctx = new InvocationContext(
            cmd.Parse("server", "namespace"));

        await CertificateGenerator.Handler(console, ctx);

        console.Output.Should().Contain($"File: {file}");
    }

    [Fact]
    public async Task Should_Generate_Valid_Certificates()
    {
        var console = new TestConsole();

        var cmd = CertificateGenerator.Command;
        var ctx = new InvocationContext(
            cmd.Parse("server", "namespace"));

        await CertificateGenerator.Handler(console, ctx);

        var output = console.Lines.ToArray();
        var caCertString = string.Join('\n', output[4..15]);
        var caCertKeyString = string.Join('\n', output[18..23]);
        var srvCertString = string.Join('\n', output[26..42]);
        var srvCertKeyString = string.Join('\n', output[45..50]);

        if (new PemReader(new StringReader(caCertString)).ReadObject() is not X509Certificate caCert)
        {
            Assert.Fail("Could not parse CA certificate.");
            return;
        }

        if (new PemReader(new StringReader(caCertKeyString)).ReadObject() is not AsymmetricCipherKeyPair caKey)
        {
            Assert.Fail("Could not parse CA private key.");
            return;
        }

        if (new PemReader(new StringReader(srvCertString)).ReadObject() is not X509Certificate srvCert)
        {
            Assert.Fail("Could not parse server certificate.");
            return;
        }

        if (new PemReader(new StringReader(srvCertKeyString)).ReadObject() is not AsymmetricCipherKeyPair)
        {
            Assert.Fail("Could not parse server private key.");
            return;
        }

        caCert.IsValidNow.Should().BeTrue();
        caCert.Verify(caKey.Public);

        srvCert.IsValidNow.Should().BeTrue();
        srvCert.Verify(caKey.Public);
    }
}
