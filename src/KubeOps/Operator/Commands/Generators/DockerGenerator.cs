﻿using System.IO;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators
{
    [Command("docker", Description = "Generates the docker file for building.")]
    internal class DockerGenerator : GeneratorBase
    {
        [Option(
            "--dotnet-tag",
            Description = @"Defines the used dotnet docker image tag for the dockerfile (default: ""latest"").")]
        public string DotnetImageTag { get; set; } = "latest";

        [Option("--solution-dir", Description = "The folder path where the solution resides.")]
        public string SolutionDir { get; set; } = string.Empty;

        [Option("--target-file", Description = "The executable file at the end.")]
        public string TargetFile { get; set; } = "<<TARGETFILE>>";

        [Option("--project-path", Description = "The path to the project file.")]
        public string ProjectPath { get; set; } = string.Empty;

        private string ProjectToBuild => Path.GetRelativePath(SolutionDir, ProjectPath).Replace('\\', '/');

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var dockerfile = GenerateDockerfile();
            if (!string.IsNullOrWhiteSpace(OutputPath))
            {
                await using var file = File.Open(OutputPath, FileMode.Create);
                await file.WriteAsync(Encoding.UTF8.GetBytes(dockerfile));
            }
            else
            {
                await app.Out.WriteLineAsync(dockerfile);
            }

            return ExitCodes.Success;
        }

        public string GenerateDockerfile() =>
            $@"# Build the operator
FROM mcr.microsoft.com/dotnet/sdk:{DotnetImageTag} as build
WORKDIR /operator

COPY ./ ./
RUN dotnet publish -c Release -o out {ProjectToBuild}

# The runner for the application
FROM mcr.microsoft.com/dotnet/aspnet:{DotnetImageTag} as final
WORKDIR /operator

COPY --from=build /operator/out/ ./

ENTRYPOINT [ ""dotnet"", ""{TargetFile}"" ]
";
    }
}
