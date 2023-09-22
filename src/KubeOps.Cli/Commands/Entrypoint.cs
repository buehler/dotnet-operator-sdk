using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Cli.Commands;

[Command(Name = "kubeops", Description = "CLI for KubeOps.", UsePagerForHelpText = true)]
[Subcommand(typeof(Generator.Generator))]
[Subcommand(typeof(Utilities.Version))]
internal class Entrypoint
{
    public int OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return ExitCodes.UsageError;
    }
}
