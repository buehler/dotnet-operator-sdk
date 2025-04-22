using System.CommandLine;
using System.Text.RegularExpressions;

using KubeOps.Cli.Output;

namespace KubeOps.Cli;

internal static class Options
{
    public static readonly Option<OutputFormat> OutputFormat = new(
        "--format",
        () => Output.OutputFormat.Yaml,
        "The format of the generated output.");

    public static readonly Option<string?> OutputPath = new(
        "--out",
        "The path the command will write the files to. If omitted, prints output to console.");

    public static readonly Option<string?> TargetFramework = new(
        ["--target-framework", "--tfm"],
        description: "Target framework of projects in the solution to search for entities. " +
                     "If omitted, the newest framework is used.");

    public static readonly Option<Regex?> SolutionProjectRegex = new(
        "--project",
        parseArgument: result =>
        {
            var value = result.Tokens.Single().Value;
            return new Regex(value);
        },
        description: "Regex pattern to filter projects in the solution to search for entities. " +
                     "If omitted, all projects are searched.");

    public static readonly Option<bool> Force = new(
        ["--force", "-f"],
        () => false,
        description: "Do not bother the user with questions and just do it.");

    public static readonly Option<bool> ClearOutputPath = new(
        ["--clear-out"],
        () => false,
        description: "Clear the output path before generating resources.");

    public static readonly Option<string> AccessibleDockerImage = new(
        "--docker-image",
        () => "accessible-docker-image",
        description: "An accessible docker image to deploy");

    public static readonly Option<string> AccessibleDockerTag = new(
        "--docker-image-tag",
        () => "latest",
        description: "Tag for an accessible docker image to deploy");
}
