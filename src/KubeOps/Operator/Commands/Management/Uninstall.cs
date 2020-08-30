﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KubeOps.Operator.Client;
using KubeOps.Operator.Commands.Generators;
using KubeOps.Operator.Entities.Extensions;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Rest;

namespace KubeOps.Operator.Commands.Management
{
    [Command(
        "uninstall",
        "u",
        Description =
            "Uninstall the current custom resources (crd's) from the cluster of the actually selected context.")]
    internal class Uninstall
    {
        private readonly IKubernetesClient _client;

        // TODO: kube proxy (for cluster stuff)
        public Uninstall(IKubernetesClient client)
        {
            _client = client;
        }

        [Option(Description = "Do not ask the user if the uninstall should proceed.")]
        public bool Force { get; set; }

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var error = false;
            var crds = CrdGenerator.GenerateCrds().ToList();
            await app.Out.WriteLineAsync($"Found {crds.Count} CRD's.");

            if (!Force && !Prompt.GetYesNo("Should the uninstall proceed?", false, ConsoleColor.Red))
            {
                return ExitCodes.Error;
            }

            await app.Out.WriteLineAsync(
                $@"Starting uninstall from the cluster with url ""{_client.ApiClient.BaseUri}"".");

            foreach (var crd in crds)
            {
                await app.Out.WriteLineAsync(
                    $@"Uninstall ""{crd.Spec.Group}/{crd.Spec.Names.Kind}"" from the cluster");
                try
                {
                    try
                    {
                        await _client.Delete(crd);
                    }
                    catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.NotFound)
                    {
                        await _client.Delete(crd.Convert());
                    }
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
}
