using System.CommandLine;
using System.CommandLine.Help;

namespace KubeOps.Cli.Commands.Generator;

internal static class Generate
{
    public static Command Command
    {
        get
        {
            var cmd = new Command("generate", "Generates elements related to an operator.")
            {
                OperatorGenerator.Command,
            };
            cmd.AddAlias("gen");
            cmd.AddAlias("g");
            cmd.SetHandler(ctx => ctx.HelpBuilder.Write(cmd, Console.Out));

            return cmd;
        }
    }
}
