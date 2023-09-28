using System.CommandLine;

namespace KubeOps.Cli;

internal static class Arguments
{
    public static readonly Argument<FileInfo?> SolutionOrProjectFile = new(
        "sln/csproj file",
        () =>
        {
            var projectFile
                = Directory.EnumerateFiles(
                        Directory.GetCurrentDirectory(),
                        "*.csproj")
                    .Select(f => new FileInfo(f))
                    .FirstOrDefault();
            var slnFile
                = Directory.EnumerateFiles(
                        Directory.GetCurrentDirectory(),
                        "*.sln")
                    .Select(f => new FileInfo(f))
                    .FirstOrDefault();

            return (projectFile, slnFile) switch
            {
                ({ } prj, _) => prj,
                (_, { } sln) => sln,
                _ => null,
            };
        },
        "A solution or project file where entities are located. " +
        "If omitted, the current directory is searched for a *.csproj or *.sln file. " +
        "If an *.sln file is used, all projects in the solution (with the newest framework) will be searched for entities. " +
        "This behaviour can be filtered by using the --project and --target-framework option.");

    public static readonly Argument<string> CertificateServerName = new(
        "name",
        "The server name for the certificate (name of the service/deployment).");

    public static readonly Argument<string> CertificateServerNamespace = new(
        "namespace",
        () => "default",
        "The Kubernetes namespace that the operator will be run.");
}
