using System.Collections.Generic;
using System.Linq;
using GlobExpressions;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Test);

    [Parameter("Version of the nuget package to build.")] readonly string Version = string.Empty;
    [Parameter("Release notes to append.")] readonly string ReleaseNotes = string.Empty;

    string NugetVersion => Version.StartsWith("v") ? Version.Substring(1) : Version;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution = new Solution();

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    IEnumerable<Project> Projects => Solution.AllProjects.Where(p => p.SolutionFolder?.Name == "src");

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetClean(s => s.SetProject(Solution));
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() => DotNetRestore(_ => _
            .SetProjectFile(Solution)));

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() => DotNetBuild(_ => _
            .SetProjectFile(Solution)
            .SetConfiguration(Configuration)
            .EnableNoRestore()));

    Target Test => _ => _
        .DependsOn(Clean, Compile)
        .Executes(() => DotNetTest(s => s
            .SetProjectFile(Solution)
            .SetConfiguration(Configuration)
            .SetProperty("CollectCoverage", true)
            .EnableNoBuild()));

    Target Pack => _ => _
        .DependsOn(Clean, Restore)
        .Requires(() => !string.IsNullOrWhiteSpace(Version))
        .Executes(() => DotNetPack(s => s
            .SetConfiguration(Configuration.Release)
            .SetVersion(NugetVersion)
            .SetPackageReleaseNotes(ReleaseNotes)
            .SetOutputDirectory(ArtifactsDirectory)
            .CombineWith(Projects, (ss, proj) => ss
                .SetProject(proj))));

    Target Publish => _ => _
        .Requires(
            () => !string.IsNullOrWhiteSpace(EnvironmentInfo.GetVariable<string>("NUGET_SERVER", null)),
            () => !string.IsNullOrWhiteSpace(EnvironmentInfo.GetVariable<string>("NUGET_KEY", null)))
        .Executes(() => DotNetNuGetPush(s => s
            .SetSource(EnvironmentInfo.GetVariable<string>("NUGET_SERVER", null))
            .SetApiKey(EnvironmentInfo.GetVariable<string>("NUGET_KEY", null))
            .CombineWith(Glob.Files(ArtifactsDirectory, "*.nupkg"), (ss, package) => ss
                .SetTargetPath(package))));

}
