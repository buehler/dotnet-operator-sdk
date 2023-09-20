using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Cli.Generator;

[Command("crd", "crds", Description = "Generates the needed CRD for kubernetes. (Aliases: crds)")]
internal class CrdGenerator
{
    public async Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        
        return ExitCodes.Success;
    }
}
