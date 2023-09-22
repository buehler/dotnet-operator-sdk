using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Cli.Commands.Generator;

[Command("generator", "gen", "g", Description = "Generates elements related to an operator. (Aliases: gen, g)")]
[Subcommand(typeof(CrdGenerator))]

// [Subcommand(typeof(DockerGenerator))]
// [Subcommand(typeof(InstallerGenerator))]
// [Subcommand(typeof(OperatorGenerator))]
// [Subcommand(typeof(RbacGenerator))]
internal class Generator
{
    public int OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return ExitCodes.UsageError;
    }
}
