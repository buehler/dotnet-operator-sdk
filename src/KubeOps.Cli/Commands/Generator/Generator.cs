using System.CommandLine;
using System.CommandLine.Help;

namespace KubeOps.Cli.Commands.Generator;

internal static class Generator
{
    public static Command Command
    {
        get
        {
            var cmd = new Command("generator", "Generates elements related to an operator.")
            {
                CrdGenerator.Command,
                RbacGenerator.Command,
            };
            cmd.AddAlias("gen");
            cmd.AddAlias("g");
            cmd.SetHandler(ctx => ctx.HelpBuilder.Write(cmd, Console.Out));

            return cmd;
        }
    }
}
