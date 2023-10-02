﻿using FluentAssertions;

using KubeOps.Generator.Generators;

using Microsoft.CodeAnalysis.CSharp;

namespace KubeOps.Generator.Test;

public class ControllerRegistrationGeneratorTest
{
    [Theory]
    [InlineData("", """
                    using KubeOps.Abstractions.Builder;
                    
                    public static class ControllerRegistrations
                    {
                        public static IOperatorBuilder RegisterControllers(this IOperatorBuilder builder)
                        {
                            return builder;
                        }
                    }
                    """)]
    [InlineData("""
                [KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
                public class V1TestEntity : IKubernetesObject<V1ObjectMeta>
                {
                }
                
                public class V1TestEntityController : IEntityController<V1TestEntity>
                {
                }
                """, """
                     using KubeOps.Abstractions.Builder;
                     
                     public static class ControllerRegistrations
                     {
                         public static IOperatorBuilder RegisterControllers(this IOperatorBuilder builder)
                         {
                             builder.AddController<global::V1TestEntityController, global::V1TestEntity>();
                             return builder;
                         }
                     }
                     """)]
    public void Should_Generate_Correct_Code(string input, string expectedResult)
    {
        var inputCompilation = input.CreateCompilation();
        expectedResult = expectedResult.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new ControllerRegistrationGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("ControllerRegistrations.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }
}
