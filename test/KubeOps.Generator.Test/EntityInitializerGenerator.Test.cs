﻿using FluentAssertions;

using KubeOps.Generator.Generators;

using Microsoft.CodeAnalysis.CSharp;

namespace KubeOps.Generator.Test;

public class EntityInitializerGeneratorTest
{
    [Fact]
    public void Should_Generate_Empty_Initializer_Without_Input()
    {
        var inputCompilation = string.Empty.CreateCompilation();
        var expectedResult = """
                             public static class EntityInitializer
                             {
                             }
                             """.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new EntityInitializerGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("EntityInitializer.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void Should_Generate_Static_Initializer_For_Non_Partial_Entities()
    {
        var inputCompilation = """
                               [KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
                               public class V1TestEntity : IKubernetesObject<V1ObjectMeta>
                               {
                               }

                               [KubernetesEntity(Group = "testing.dev", ApiVersion = "v2", Kind = "TestEntity")]
                               public class V2TestEntity : IKubernetesObject<V1ObjectMeta>
                               {
                               }
                               """.CreateCompilation();
        var expectedResult = """
                             public static class EntityInitializer
                             {
                                 public static global::V1TestEntity Initialize(this global::V1TestEntity entity)
                                 {
                                     entity.ApiVersion = "testing.dev/v1";
                                     entity.Kind = "TestEntity";
                                     return entity;
                                 }
                             
                                 public static global::V2TestEntity Initialize(this global::V2TestEntity entity)
                                 {
                                     entity.ApiVersion = "testing.dev/v2";
                                     entity.Kind = "TestEntity";
                                     return entity;
                                 }
                             }
                             """.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new EntityInitializerGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        output.SyntaxTrees.Any(s => s.FilePath.Contains("V1TestEntity")).Should().BeFalse();
        output.SyntaxTrees.Any(s => s.FilePath.Contains("V2TestEntity")).Should().BeFalse();
        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("EntityInitializer.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void Should_Generate_Correct_Initializer_Entities_Without_Groups()
    {
        var inputCompilation = """
                               [KubernetesEntity(ApiVersion = "v1", Kind = "ConfigMap")]
                               public class V1ConfigMap : IKubernetesObject<V1ObjectMeta>
                               {
                               }
                               """.CreateCompilation();
        var expectedResult = """
                             public static class EntityInitializer
                             {
                                 public static global::V1ConfigMap Initialize(this global::V1ConfigMap entity)
                                 {
                                     entity.ApiVersion = "v1";
                                     entity.Kind = "ConfigMap";
                                     return entity;
                                 }
                             }
                             """.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new EntityInitializerGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        output.SyntaxTrees.Any(s => s.FilePath.Contains("V1ConfigMap")).Should().BeFalse();
        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("EntityInitializer.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void Should_Generate_Static_Initializer_For_Partial_Entity_With_Default_Ctor()
    {
        var inputCompilation = """
                               [KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
                               public partial class V1TestEntity : IKubernetesObject<V1ObjectMeta>
                               {
                                   public V1TestEntity(){}
                               }
                               """.CreateCompilation();
        var expectedResult = """
                             public static class EntityInitializer
                             {
                                 public static global::V1TestEntity Initialize(this global::V1TestEntity entity)
                                 {
                                     entity.ApiVersion = "testing.dev/v1";
                                     entity.Kind = "TestEntity";
                                     return entity;
                                 }
                             }
                             """.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new EntityInitializerGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        output.SyntaxTrees.Any(s => s.FilePath.Contains("V1TestEntity")).Should().BeFalse();
        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("EntityInitializer.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void Should_Not_Generate_Static_Initializer_For_Partial_Entity()
    {
        var inputCompilation = """
                               [KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
                               public partial class V1TestEntity : IKubernetesObject<V1ObjectMeta>
                               {
                               }
                               """.CreateCompilation();
        var expectedResult = """
                             public static class EntityInitializer
                             {
                             }
                             """.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new EntityInitializerGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        output.SyntaxTrees.Any(s => s.FilePath.Contains("V1TestEntity")).Should().BeTrue();
        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("EntityInitializer.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void Should_Generate_Default_Ctor_For_FileNamespaced_Partial_Entity()
    {
        var inputCompilation = """
                               namespace Foo.Bar;
                               [KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
                               public partial class V1TestEntity : IKubernetesObject<V1ObjectMeta>
                               {
                               }
                               """.CreateCompilation();
        var expectedResult = """
                             namespace Foo.Bar;
                             public partial class V1TestEntity
                             {
                                 public V1TestEntity()
                                 {
                                     ApiVersion = "testing.dev/v1";
                                     Kind = "TestEntity";
                                 }
                             }
                             """.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new EntityInitializerGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("V1TestEntity.init.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void Should_Generate_Default_Ctor_For_ScopeNamespaced_Partial_Entity()
    {
        var inputCompilation = """
                               namespace Foo.Bar
                               {
                                   namespace Baz 
                                   {
                                       [KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
                                       public partial class V1TestEntity : IKubernetesObject<V1ObjectMeta>
                                       {
                                       }
                                   }
                               }
                               """.CreateCompilation();
        var expectedResult = """
                             namespace Foo.Bar.Baz;
                             public partial class V1TestEntity
                             {
                                 public V1TestEntity()
                                 {
                                     ApiVersion = "testing.dev/v1";
                                     Kind = "TestEntity";
                                 }
                             }
                             """.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new EntityInitializerGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("V1TestEntity.init.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void Should_Generate_Default_Ctor_For_Global_Partial_Entity()
    {
        var inputCompilation = """
                               [KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
                               public partial class V1TestEntity : IKubernetesObject<V1ObjectMeta>
                               {
                               }
                               """.CreateCompilation();
        var expectedResult = """
                             public partial class V1TestEntity
                             {
                                 public V1TestEntity()
                                 {
                                     ApiVersion = "testing.dev/v1";
                                     Kind = "TestEntity";
                                 }
                             }
                             """.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new EntityInitializerGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("V1TestEntity.init.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void Should_Generate_Default_Ctor_For_Partial_Entity_With_Ctor()
    {
        var inputCompilation = """
                               [KubernetesEntity(Group = "testing.dev", ApiVersion = "v1", Kind = "TestEntity")]
                               public partial class V1TestEntity : IKubernetesObject<V1ObjectMeta>
                               {
                                   public V1TestEntity(string name){}
                               }
                               """.CreateCompilation();
        var expectedResult = """
                             public partial class V1TestEntity
                             {
                                 public V1TestEntity()
                                 {
                                     ApiVersion = "testing.dev/v1";
                                     Kind = "TestEntity";
                                 }
                             }
                             """.ReplaceLineEndings();

        var driver = CSharpGeneratorDriver.Create(new EntityInitializerGenerator());
        driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var output, out var diag);

        var result = output.SyntaxTrees
            .First(s => s.FilePath.Contains("V1TestEntity.init.g.cs"))
            .ToString().ReplaceLineEndings();
        result.Should().Be(expectedResult);
    }
}
