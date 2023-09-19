using KubeOps.Operator.Serialization;

using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators;

internal abstract class OutputBase
{
    [Option(
        CommandOptionType.SingleValue,
        Description = "Sets the output format for the generator.")]
    public SerializerOutputFormat Format { get; set; }

    [Option(
        Description = @"The path the command will write the files to. If empty, prints output to console.",
        LongName = "out")]
    public string? OutputPath { get; set; }
}
