using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;

using KubeOps.Cli.Output;

using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

using Spectre.Console;

namespace KubeOps.Cli.Commands.Generator;

internal static class CertificateGenerator
{
    public static Command Command
    {
        get
        {
            var cmd = new Command("certificates", "Generates a CA and a server certificate.")
            {
                Options.OutputPath, Arguments.CertificateServerName, Arguments.CertificateServerNamespace,
            };
            cmd.AddAlias("cert");
            cmd.SetHandler(ctx => Handler(AnsiConsole.Console, ctx));

            return cmd;
        }
    }

    internal static async Task Handler(IAnsiConsole console, InvocationContext ctx)
    {
        var outPath = ctx.ParseResult.GetValueForOption(Options.OutputPath);
        var result = new ResultOutput(console, OutputFormat.Plain);

        console.MarkupLine("Generate [cyan]CA[/] certificate and private key.");
        var (caCert, caKey) = CreateCaCertificate();

        result.Add("ca.pem", ToPem(caCert));
        result.Add("ca-key.pem", ToPem(caKey));

        console.MarkupLine("Generate [cyan]server[/] certificate and private key.");
        var (srvCert, srvKey) = CreateServerCertificate(
            (caCert, caKey),
            ctx.ParseResult.GetValueForArgument(Arguments.CertificateServerName),
            ctx.ParseResult.GetValueForArgument(Arguments.CertificateServerNamespace));

        result.Add("svc.pem", ToPem(srvCert));
        result.Add("svc-key.pem", ToPem(srvKey));

        if (outPath is not null)
        {
            await result.Write(outPath);
        }
        else
        {
            result.Write();
        }
    }

    private static string ToPem(object obj)
    {
        var sb = new StringBuilder();
        using var writer = new PemWriter(new StringWriter(sb));
        writer.WriteObject(obj);
        return sb.ToString();
    }

    private static (X509Certificate Certificate, AsymmetricCipherKeyPair Key) CreateCaCertificate()
    {
        var randomGenerator = new CryptoApiRandomGenerator();
        var random = new SecureRandom(randomGenerator);

        // The Certificate Generator
        var certificateGenerator = new X509V3CertificateGenerator();

        // Serial Number
        var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
        certificateGenerator.SetSerialNumber(serialNumber);

        // Issuer and Subject Name
        var name = new X509Name("CN=Operator Root CA, C=DEV, L=Kubernetes");
        certificateGenerator.SetIssuerDN(name);
        certificateGenerator.SetSubjectDN(name);

        // Valid For
        var notBefore = DateTime.UtcNow.Date;
        var notAfter = notBefore.AddYears(5);
        certificateGenerator.SetNotBefore(notBefore);
        certificateGenerator.SetNotAfter(notAfter);

        // Cert Extensions
        certificateGenerator.AddExtension(
            X509Extensions.BasicConstraints,
            true,
            new BasicConstraints(true));
        certificateGenerator.AddExtension(
            X509Extensions.KeyUsage,
            true,
            new KeyUsage(KeyUsage.KeyCertSign | KeyUsage.CrlSign | KeyUsage.KeyEncipherment));

        // Subject Public Key
        const int keyStrength = 256;
        var keyGenerator = new ECKeyPairGenerator("ECDSA");
        keyGenerator.Init(new KeyGenerationParameters(random, keyStrength));
        var key = keyGenerator.GenerateKeyPair();

        certificateGenerator.SetPublicKey(key.Public);

        var signatureFactory = new Asn1SignatureFactory("SHA512WITHECDSA", key.Private, random);
        var certificate = certificateGenerator.Generate(signatureFactory);

        return (certificate, key);
    }

    private static (X509Certificate Certificate, AsymmetricCipherKeyPair Key) CreateServerCertificate(
        (X509Certificate Certificate, AsymmetricCipherKeyPair Key) ca, string serverName, string serverNamespace)
    {
        var randomGenerator = new CryptoApiRandomGenerator();
        var random = new SecureRandom(randomGenerator);

        // The Certificate Generator
        var certificateGenerator = new X509V3CertificateGenerator();

        // Serial Number
        var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
        certificateGenerator.SetSerialNumber(serialNumber);

        // Issuer and Subject Name
        certificateGenerator.SetIssuerDN(ca.Certificate.SubjectDN);
        certificateGenerator.SetSubjectDN(new X509Name("CN=Operator Service, C=DEV, L=Kubernetes"));

        // Valid For
        var notBefore = DateTime.UtcNow.Date;
        var notAfter = notBefore.AddYears(5);
        certificateGenerator.SetNotBefore(notBefore);
        certificateGenerator.SetNotAfter(notAfter);

        // Cert Extensions
        certificateGenerator.AddExtension(
            X509Extensions.BasicConstraints,
            false,
            new BasicConstraints(false));
        certificateGenerator.AddExtension(
            X509Extensions.KeyUsage,
            true,
            new KeyUsage(KeyUsage.NonRepudiation | KeyUsage.KeyEncipherment | KeyUsage.DigitalSignature));
        certificateGenerator.AddExtension(
            X509Extensions.ExtendedKeyUsage,
            false,
            new ExtendedKeyUsage(KeyPurposeID.id_kp_clientAuth, KeyPurposeID.id_kp_serverAuth));
        certificateGenerator.AddExtension(
            X509Extensions.SubjectKeyIdentifier,
            false,
            new SubjectKeyIdentifierStructure(ca.Key.Public));
        certificateGenerator.AddExtension(
            X509Extensions.AuthorityKeyIdentifier,
            false,
            new AuthorityKeyIdentifierStructure(ca.Certificate));
        certificateGenerator.AddExtension(
            X509Extensions.SubjectAlternativeName,
            false,
            new GeneralNames(new[]
            {
                new GeneralName(GeneralName.DnsName, $"{serverName}.{serverNamespace}.svc"),
                new GeneralName(GeneralName.DnsName, $"*.{serverNamespace}.svc"),
                new GeneralName(GeneralName.DnsName, "*.svc"),
            }));

        // Subject Public Key
        const int keyStrength = 256;
        var keyGenerator = new ECKeyPairGenerator("ECDSA");
        keyGenerator.Init(new KeyGenerationParameters(random, keyStrength));
        var key = keyGenerator.GenerateKeyPair();

        certificateGenerator.SetPublicKey(key.Public);

        var signatureFactory = new Asn1SignatureFactory("SHA512WITHECDSA", ca.Key.Private, random);
        var certificate = certificateGenerator.Generate(signatureFactory);

        return (certificate, key);
    }
}
