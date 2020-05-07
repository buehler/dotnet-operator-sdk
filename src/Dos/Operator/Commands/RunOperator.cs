using System.Threading.Tasks;
using Dos.Operator.Commands.Generators;
using Dos.Operator.Commands.Management;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;

namespace Dos.Operator.Commands
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
