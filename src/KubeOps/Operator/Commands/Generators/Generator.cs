using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators;

[Command("generator", "gen", "g", Description = "Generates elements related to this operator.")]
[Subcommand(typeof(CrdGenerator))]
[Subcommand(typeof(DockerGenerator))]
[Subcommand(typeof(InstallerGenerator))]
[Subcommand(typeof(OperatorGenerator))]
[Subcommand(typeof(RbacGenerator))]
internal class Generator : GeneratorBase
{
    public Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(ExitCodes.Error);
    }
}
