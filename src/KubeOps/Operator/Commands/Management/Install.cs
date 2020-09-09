using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using k8s.Models;
using KubeOps.Operator.Client;
using KubeOps.Operator.Commands.Generators;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Rest;

namespace KubeOps.Operator.Commands.Management
{
    [Command(
        "install",
        "i",
        Description =
            "Install the current custom resources (crd's) into the cluster of the actually selected context.")]
    internal class Install
    {
        private readonly IKubernetesClient _client;

        // TODO: kube proxy (for cluster stuff)
        public Install(IKubernetesClient client)
        {
            _client = client;
        }

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var error = false;
            var crds = CrdGenerator.GenerateCrds().ToList();
            await app.Out.WriteLineAsync($"Found {crds.Count} CRD's.");
            await app.Out.WriteLineAsync($@"Starting install into cluster with url ""{_client.ApiClient.BaseUri}"".");

            foreach (var crd in crds)
            {
                await app.Out.WriteLineAsync(
                    $@"Install ""{crd.Spec.Group}/{crd.Spec.Names.Kind}"" into the cluster");

                try
                {
                    try
                    {
                        await _client.Save(crd);
                    }
                    catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
                    {
                        await _client.Save((V1beta1CustomResourceDefinition)crd);
                    }
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
}
