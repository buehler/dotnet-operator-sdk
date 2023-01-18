using k8s.Autorest;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Entities;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Management;

[Command(
    "install",
    "i",
    Description =
        "Install the current custom resources (crd's) into the cluster of the actually selected context.")]
internal class Install
{
    private readonly ICrdBuilder _crdBuilder;

    public Install(ICrdBuilder crdBuilder)
    {
        _crdBuilder = crdBuilder;
    }

    public async Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        var client = app.GetRequiredService<IKubernetesClient>();

        var error = false;
        var crds = _crdBuilder.BuildCrds().ToList();
        await app.Out.WriteLineAsync($"Found {crds.Count} CRD's.");
        await app.Out.WriteLineAsync($@"Starting install into cluster with url ""{client.BaseUri}"".");

        foreach (var crd in crds)
        {
            await app.Out.WriteLineAsync(
                $@"Install ""{crd.Spec.Group}/{crd.Spec.Names.Kind}"" into the cluster");

            try
            {
                await client.Save(crd);
            }
            catch (HttpOperationException e)
            {
                await app.Out.WriteLineAsync(
                    $@"There was a http (api) error while installing ""{crd.Spec.Group}/{crd.Spec.Names.Kind}"".");
                await app.Out.WriteLineAsync(e.Message);
                await app.Out.WriteLineAsync(e.Response.Content);
                error = true;
            }
            catch (Exception e)
            {
                await app.Out.WriteLineAsync(
                    $@"There was an error while installing ""{crd.Spec.Group}/{crd.Spec.Names.Kind}"".");
                await app.Out.WriteLineAsync(e.Message);
                error = true;
            }
        }

        return error ? ExitCodes.Error : ExitCodes.Success;
    }
}
