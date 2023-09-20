using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Cli.Generator;

[Command("generator", "gen", "g", Description = "Generates elements related to an operator. (Aliases: gen, g)")]
[Subcommand(typeof(CrdGenerator))]
// [Subcommand(typeof(DockerGenerator))]
// [Subcommand(typeof(InstallerGenerator))]
// [Subcommand(typeof(OperatorGenerator))]
// [Subcommand(typeof(RbacGenerator))]
internal class Generator
{
    public Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(ExitCodes.UsageError);
    }
}
