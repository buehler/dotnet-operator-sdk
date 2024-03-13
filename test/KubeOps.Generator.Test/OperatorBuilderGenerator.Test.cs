using FluentAssertions;

using KubeOps.Generator.Generators;

using Microsoft.CodeAnalysis.CSharp;

namespace KubeOps.Generator.Test;

public class OperatorBuilderGeneratorTest
{
    [Fact]
    public void Should_Generate_Correct_Code()
    {
        var inputCompilation = string.Empty.CreateCompilation();
        var expectedResult =
            """
                using KubeOps.Abstractions.Builder;

                public static class OperatorBuilderExtensions
                {
                    public static IOperatorBuilder RegisterComponents(this IOperatorBuilder builder)
                    {
                        builder.RegisterControllers();
                        builder.RegisterFinalizers();
                        return builder;
                    }
                }
                """.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new OperatorBuilderGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("OperatorBuilder.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }
}
