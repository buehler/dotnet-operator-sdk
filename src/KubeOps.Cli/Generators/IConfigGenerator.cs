using KubeOps.Cli.Output;

using Spectre.Console;

namespace KubeOps.Cli.Generators;

internal interface IConfigGenerator
{
    void Generate(ResultOutput output);
}
