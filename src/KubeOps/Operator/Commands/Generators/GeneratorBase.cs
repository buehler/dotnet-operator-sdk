using KubeOps.Operator.Serialization;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators;

internal abstract class GeneratorBase
{
    [Option(CommandOptionType.SingleValue, Description = "Determines the output format for the generator.")]
    public SerializerOutputFormat Format { get; set; }

    [Option(
        Description = @"The ""root"" path for the generator to put files in - if empty, prints to console.",
        LongName = "out")]
    public string? OutputPath { get; set; }
}
