using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Management.Webhooks;

[Command("webhooks", "wh", Description = "Management command for admission webhooks (validation / mutation).")]
[Subcommand(typeof(Register))]
[Subcommand(typeof(Install))]
internal class Webhooks
{
    public Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(ExitCodes.Error);
    }
}
