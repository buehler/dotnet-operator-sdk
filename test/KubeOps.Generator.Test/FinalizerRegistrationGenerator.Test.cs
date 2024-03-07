﻿using FluentAssertions;

using KubeOps.Generator.Generators;

using Microsoft.CodeAnalysis.CSharp;

namespace KubeOps.Generator.Test;

public class FinalizerRegistrationGeneratorTest
{
    [Theory]
    [InlineData("", """
                    using KubeOps.Abstractions.Builder;

                    public static class FinalizerRegistrations
                    {
                        public static IOperatorBuilder RegisterFinalizers(this IOperatorBuilder builder)
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

                public class V1TestEntityFinalizer : IEntityFinalizer<V1TestEntity>
                {
                }
                """, """
                     using KubeOps.Abstractions.Builder;

                     public static class FinalizerRegistrations
                     {
                         public const string V1TestEntityFinalizerIdentifier = "testing.dev/v1testentityfinalizer";
                         public static IOperatorBuilder RegisterFinalizers(this IOperatorBuilder builder)
                         {
                             builder.AddFinalizer<global::V1TestEntityFinalizer, global::V1TestEntity>(V1TestEntityFinalizerIdentifier);
                             return builder;
                         }
                     }
                     """)]
    [InlineData("""
                [KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
                public class V1TestEntity : IKubernetesObject<V1ObjectMeta>
                {
                }

                public class V1TestEntityCleanupDeployment : IEntityFinalizer<V1TestEntity>
                {
                }

                public class V1TestEntityCleanupOtherResourcesSuchThatThisFinalizerHasAVeryLongName : IEntityFinalizer<V1TestEntity>
                {
                }
                """, """
                     using KubeOps.Abstractions.Builder;

                     public static class FinalizerRegistrations
                     {
                         public const string V1TestEntityCleanupDeploymentIdentifier = "testing.dev/v1testentitycleanupdeploymentfinalizer";
                         public const string V1TestEntityCleanupOtherResourcesSuchThatThisFinalizerHasAVeryLongNameIdentifier = "testing.dev/v1testentitycleanupotherresourcessuchthatthisfinali";
                         public static IOperatorBuilder RegisterFinalizers(this IOperatorBuilder builder)
                         {
                             builder.AddFinalizer<global::V1TestEntityCleanupDeployment, global::V1TestEntity>(V1TestEntityCleanupDeploymentIdentifier);
                             builder.AddFinalizer<global::V1TestEntityCleanupOtherResourcesSuchThatThisFinalizerHasAVeryLongName, global::V1TestEntity>(V1TestEntityCleanupOtherResourcesSuchThatThisFinalizerHasAVeryLongNameIdentifier);
                             return builder;
                         }
                     }
                     """)]
    public void Should_Generate_Correct_Code(string input, string expectedResult)
    {
        var inputCompilation = input.CreateCompilation();
        expectedResult = expectedResult.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new FinalizerRegistrationGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("FinalizerRegistrations.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }
}
