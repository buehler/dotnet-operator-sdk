using System.CommandLine;
using System.CommandLine.Invocation;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Kustomize;
using KubeOps.Cli.Output;

using Spectre.Console;

namespace KubeOps.Cli.Commands.Generator;

internal static class DockerGenerator
{
    public static Command Command
    {
        get
        {
            var cmd = new Command("docker", "Generates a dockerfile that builds and starts the operator.")
            {
                Options.OutputPath,
            };
            cmd.SetHandler(ctx => Handler(AnsiConsole.Console, ctx));

            return cmd;
        }
    }

    internal static async Task Handler(IAnsiConsole console, InvocationContext ctx)
    {
        var outPath = ctx.ParseResult.GetValueForOption(Options.OutputPath);

        var result = new ResultOutput(console, OutputFormat.Plain);
        console.WriteLine("Generate operator Dockerfile.");

        result.Add(
            "Dockerfile",
            """
            FROM mcr.microsoft.com/dotnet/sdk:latest as build
            WORKDIR /operator

            COPY ./ ./
            RUN dotnet publish -c Release /p:AssemblyName=operator -o out

            # The runner for the application
            FROM mcr.microsoft.com/dotnet/runtime:latest as final

            RUN addgroup k8s-operator && useradd -G k8s-operator operator-user

            WORKDIR /operator
            COPY --from=build /operator/out/ ./
            RUN chown operator-user:k8s-operator -R .

            USER operator-user

            ENTRYPOINT [ "dotnet", "operator.dll" ]
            """);

        if (outPath is not null)
        {
            await result.Write(outPath);
        }
        else
        {
            result.Write();
        }
    }
}
