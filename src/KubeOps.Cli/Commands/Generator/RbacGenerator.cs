using System.CommandLine;
using System.CommandLine.Invocation;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Kustomize;
using KubeOps.Abstractions.Rbac;
using KubeOps.Cli.Output;
using KubeOps.Cli.Transpilation;
using KubeOps.Transpiler;

using Spectre.Console;

namespace KubeOps.Cli.Commands.Generator;

internal static class RbacGenerator
{
    public static Command Command
    {
        get
        {
            var cmd = new Command("rbac", "Generates rbac roles for the operator project or solution.")
            {
                Options.OutputFormat,
                Options.OutputPath,
                Options.SolutionProjectRegex,
                Options.TargetFramework,
                Arguments.SolutionOrProjectFile,
            };
            cmd.SetHandler(ctx => Handler(AnsiConsole.Console, ctx));

            return cmd;
        }
    }

    internal static async Task Handler(IAnsiConsole console, InvocationContext ctx)
    {
        var file = ctx.ParseResult.GetValueForArgument(Arguments.SolutionOrProjectFile);
        var outPath = ctx.ParseResult.GetValueForOption(Options.OutputPath);
        var format = ctx.ParseResult.GetValueForOption(Options.OutputFormat);

        var parser = file switch
        {
            { Extension: ".csproj", Exists: true } => await AssemblyLoader.ForProject(console, file),
            { Extension: ".sln", Exists: true } => await AssemblyLoader.ForSolution(
                console,
                file,
                ctx.ParseResult.GetValueForOption(Options.SolutionProjectRegex),
                ctx.ParseResult.GetValueForOption(Options.TargetFramework)),
            { Exists: false } => throw new FileNotFoundException($"The file {file.Name} does not exist."),
            _ => throw new NotSupportedException("Only *.csproj and *.sln files are supported."),
        };
        var result = new ResultOutput(console, format);
        console.WriteLine($"Generate RBAC roles for {file.Name}.");

        var attributes = parser
            .GetRbacAttributes()
            .Concat(parser.GetContextType<DefaultRbacAttributes>().GetCustomAttributesData<EntityRbacAttribute>())
            .ToList();

        var role = new V1ClusterRole(rules: parser.Transpile(attributes).ToList()).Initialize();
        role.Metadata.Name = "operator-role";
        result.Add($"operator-role.{format.ToString().ToLowerInvariant()}", role);

        var roleBinding = new V1ClusterRoleBinding(
                roleRef: new V1RoleRef(V1ClusterRole.KubeGroup, V1ClusterRole.KubeKind, "operator-role"),
                subjects: new List<V1Subject>
                {
                    new(V1ServiceAccount.KubeKind, "default", namespaceProperty: "system"),
                })
            .Initialize();
        roleBinding.Metadata.Name = "operator-role-binding";
        result.Add($"operator-role-binding.{format.ToString().ToLowerInvariant()}", roleBinding);

        result.Add(
            $"kustomization.{format.ToString().ToLowerInvariant()}",
            new KustomizationConfig
            {
                Resources = new List<string>
                {
                    $"operator-role.{format.ToString().ToLowerInvariant()}",
                    $"operator-role-binding.{format.ToString().ToLowerInvariant()}",
                },
                CommonLabels = new Dictionary<string, string> { { "operator-element", "rbac" }, },
            });

        if (outPath is not null)
        {
            await result.Write(outPath);
        }
        else
        {
            result.Write();
        }
    }

    [EntityRbac(typeof(Corev1Event), Verbs = RbacVerb.Get | RbacVerb.List | RbacVerb.Create | RbacVerb.Update)]
    [EntityRbac(typeof(V1Lease), Verbs = RbacVerb.All)]
    private sealed class DefaultRbacAttributes;
}
