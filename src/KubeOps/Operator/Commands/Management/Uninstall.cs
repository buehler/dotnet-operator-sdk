using k8s.Autorest;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Entities;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Management;

[Command(
    "uninstall",
    "u",
    Description =
        "Uninstall the current custom resources (crd's) from the cluster of the actually selected context.")]
internal class Uninstall
{
    private readonly ICrdBuilder _crdBuilder;

    public Uninstall(ICrdBuilder crdBuilder)
    {
        _crdBuilder = crdBuilder;
    }

    [Option(Description = "Do not ask the user if the uninstall should proceed.")]
    public bool Force { get; set; }

    public async Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        var client = app.GetRequiredService<IKubernetesClient>();

        var error = false;
        var crds = _crdBuilder.BuildCrds().ToList();
        await app.Out.WriteLineAsync($"Found {crds.Count} CRD's.");

        if (!Force && !Prompt.GetYesNo("Should the uninstall proceed?", false, ConsoleColor.Red))
        {
            return ExitCodes.Error;
        }

        await app.Out.WriteLineAsync(
            $@"Starting uninstall from the cluster with url ""{client.BaseUri}"".");

        foreach (var crd in crds)
        {
            await app.Out.WriteLineAsync(
                $@"Uninstall ""{crd.Spec.Group}/{crd.Spec.Names.Kind}"" from the cluster");
            try
            {
                await client.Delete(crd);
            }
            catch (HttpOperationException e)
            {
                await app.Out.WriteLineAsync(
                    $@"There was a http (api) error while uninstalling ""{crd.Spec.Group}/{crd.Spec.Names.Kind}"".");
                await app.Out.WriteLineAsync(e.Message);
                await app.Out.WriteLineAsync(e.Response.Content);
                error = true;
            }
            catch (Exception e)
            {
                await app.Out.WriteLineAsync(
                    $@"There was an error while uninstalling ""{crd.Spec.Group}/{crd.Spec.Names.Kind}"".");
                await app.Out.WriteLineAsync(e.Message);
                error = true;
            }
        }

        return error ? ExitCodes.Error : ExitCodes.Success;
    }
}
