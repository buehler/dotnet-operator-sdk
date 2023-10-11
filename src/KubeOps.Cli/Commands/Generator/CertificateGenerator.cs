using System.CommandLine;
using System.CommandLine.Invocation;

using KubeOps.Cli.Certificates;
using KubeOps.Cli.Output;

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
        var (caCert, caKey) = Certificates.CertificateGenerator.CreateCaCertificate();

        result.Add("ca.pem", caCert.ToPem());
        result.Add("ca-key.pem", caKey.ToPem());

        console.MarkupLine("Generate [cyan]server[/] certificate and private key.");
        var (srvCert, srvKey) = Certificates.CertificateGenerator.CreateServerCertificate(
            (caCert, caKey),
            ctx.ParseResult.GetValueForArgument(Arguments.CertificateServerName),
            ctx.ParseResult.GetValueForArgument(Arguments.CertificateServerNamespace));

        result.Add("svc.pem", srvCert.ToPem());
        result.Add("svc-key.pem", srvKey.ToPem());

        if (outPath is not null)
        {
            await result.Write(outPath);
        }
        else
        {
            result.Write();
        }
    }
}
