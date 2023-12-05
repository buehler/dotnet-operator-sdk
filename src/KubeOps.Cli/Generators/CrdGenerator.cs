using System.Reflection;

using KubeOps.Cli.Output;
using KubeOps.Cli.Transpilation;
using KubeOps.Transpiler;

namespace KubeOps.Cli.Generators;

internal class CrdGenerator(MetadataLoadContext parser,
    OutputFormat outputFormat) : IConfigGenerator
{
    public void Generate(ResultOutput output)
    {
        var crds = parser.Transpile(parser.GetEntities()).ToList();
        foreach (var crd in crds)
        {
            output.Add($"{crd.Metadata.Name.Replace('.', '_')}.{outputFormat.GetFileExtension()}", crd);
        }
    }
}
