using KubeOps.Cli.Certificates;
using KubeOps.Cli.Output;

namespace KubeOps.Cli.Generators;

internal class CertificateGenerator(string serverName, string namespaceName) : IConfigGenerator
{
    public void Generate(ResultOutput output)
    {
        var (caCert, caKey) = Certificates.CertificateGenerator.CreateCaCertificate();

        output.Add("ca.pem", caCert.ToPem(), OutputFormat.Plain);
        output.Add("ca-key.pem", caKey.ToPem(), OutputFormat.Plain);

        var (srvCert, srvKey) = Certificates.CertificateGenerator.CreateServerCertificate(
            (caCert, caKey),
            serverName,
            namespaceName);

        output.Add("svc.pem", srvCert.ToPem(), OutputFormat.Plain);
        output.Add("svc-key.pem", srvKey.ToPem(), OutputFormat.Plain);
    }
}
