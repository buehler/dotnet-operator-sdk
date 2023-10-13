using FluentAssertions;

using KubeOps.Generator.Generators;

using Microsoft.CodeAnalysis.CSharp;

namespace KubeOps.Generator.Test;

public class EntityDefinitionGeneratorTest
{
    [Theory]
    [InlineData("", """
                    using KubeOps.Abstractions.Builder;
                    using KubeOps.Abstractions.Entities;

                    public static class EntityDefinitions
                    {
                        public static IOperatorBuilder RegisterEntities(this IOperatorBuilder builder)
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
                """, """
                     using KubeOps.Abstractions.Builder;
                     using KubeOps.Abstractions.Entities;

                     public static class EntityDefinitions
                     {
                         public static readonly EntityMetadata V1TestEntity = new("TestEntity", "v1", "testing.dev", null);
                         public static IOperatorBuilder RegisterEntities(this IOperatorBuilder builder)
                         {
                             builder.AddEntityClient<global::V1TestEntity>(V1TestEntity);
                             return builder;
                         }
                     }
                     """)]
    [InlineData("""
                [KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
                public class V1TestEntity : IKubernetesObject<V1ObjectMeta>
                {
                }
                
                [KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "AnotherEntity")]
                public class V1AnotherEntity : IKubernetesObject<V1ObjectMeta>
                {
                }
                """, """
                     using KubeOps.Abstractions.Builder;
                     using KubeOps.Abstractions.Entities;

                     public static class EntityDefinitions
                     {
                         public static readonly EntityMetadata V1TestEntity = new("TestEntity", "v1", "testing.dev", null);
                         public static readonly EntityMetadata V1AnotherEntity = new("AnotherEntity", "v1", "testing.dev", null);
                         public static IOperatorBuilder RegisterEntities(this IOperatorBuilder builder)
                         {
                             builder.AddEntityClient<global::V1TestEntity>(V1TestEntity);
                             builder.AddEntityClient<global::V1AnotherEntity>(V1AnotherEntity);
                             return builder;
                         }
                     }
                     """)]
    public void Should_Generate_Correct_Code(string input, string expectedResult)
    {
        var inputCompilation = input.CreateCompilation();
        expectedResult = expectedResult.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new EntityDefinitionGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("EntityDefinitions.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }
}
