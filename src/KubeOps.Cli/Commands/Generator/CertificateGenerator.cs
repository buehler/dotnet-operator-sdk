using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

using KubeOps.Abstractions.Kustomize;
using KubeOps.Cli.Output;
using KubeOps.Cli.Roslyn;

using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;

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

            };
            cmd.AddAlias("cert");
            cmd.SetHandler(ctx => Handler(AnsiConsole.Console, ctx));

            return cmd;
        }
    }

    internal static async Task Handler(IAnsiConsole console, InvocationContext ctx)
    {
        var randomGenerator = new CryptoApiRandomGenerator();
        var random = new SecureRandom(randomGenerator);
    }
}
