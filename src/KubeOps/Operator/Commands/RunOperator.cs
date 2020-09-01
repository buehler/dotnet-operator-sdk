﻿using System.Threading.Tasks;
using KubeOps.Operator.Commands.Generators;
using KubeOps.Operator.Commands.Management;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Commands
{
    [Command(Description = "Runs the operator.")]
    [Subcommand(typeof(Generator))]
    [Subcommand(typeof(Install))]
    [Subcommand(typeof(Uninstall))]
    internal class RunOperator
    {
        private readonly IHost _host;

        public RunOperator(IHost host)
        {
            _host = host;
        }

        public Task OnExecuteAsync() => _host.RunAsync();
    }
}
