﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s.Models;
using KubeOps.Operator.Entities;
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

        private readonly ICrdBuilder _crdBuilder;

        public Install(IKubernetesClient client, ICrdBuilder crdBuilder)
        {
            _client = client;
            _crdBuilder = crdBuilder;
        }

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var error = false;
            var crds = _crdBuilder.BuildCrds().ToList();
            await app.Out.WriteLineAsync($"Found {crds.Count} CRD's.");
            await app.Out.WriteLineAsync($@"Starting install into cluster with url ""{_client.ApiClient.BaseUri}"".");

            foreach (var crd in crds)
            {
                await app.Out.WriteLineAsync(
                    $@"Install ""{crd.Spec.Group}/{crd.Spec.Names.Kind}"" into the cluster");

                try
                {
                    await _client.Save(crd);
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
