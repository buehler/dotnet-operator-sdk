using FluentAssertions;
using k8s.Models;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Entities;
using KubeOps.Test.TestEntities;
using Xunit;

namespace KubeOps.Test.Operator.Generators;

public class CrdGeneratorTest
{
    private readonly IEnumerable<V1CustomResourceDefinition> _crds;

    public CrdGeneratorTest()
    {
        var componentRegistrar = new ComponentRegistrar();

        componentRegistrar.RegisterEntity<TestIgnoredEntity>();
        componentRegistrar.RegisterEntity<TestInvalidEntity>();
        componentRegistrar.RegisterEntity<TestSpecEntity>();
        componentRegistrar.RegisterEntity<TestClusterSpecEntity>();
        componentRegistrar.RegisterEntity<TestStatusEntity>();
        componentRegistrar.RegisterEntity<V1Alpha1VersionedEntity>();
        componentRegistrar.RegisterEntity<V1AttributeVersionedEntity>();
        componentRegistrar.RegisterEntity<V1Beta1VersionedEntity>();
        componentRegistrar.RegisterEntity<V1VersionedEntity>();
        componentRegistrar.RegisterEntity<V2AttributeVersionedEntity>();
        componentRegistrar.RegisterEntity<V2Beta2VersionedEntity>();
        componentRegistrar.RegisterEntity<V2VersionedEntity>();

        // Should be ignored since V1Pod is from the k8s assembly.
        componentRegistrar.RegisterEntity<V1Pod>();

        _crds = new CrdBuilder(componentRegistrar).BuildCrds();
    }

    [Fact]
    public void Should_Generate_Correct_Number_Of_Crds()
    {
        _crds.Count().Should().Be(5);
    }

    [Fact]
    public void Should_Not_Contain_Ignored_Entities()
    {
        _crds.Should()
            .NotContain(crd => crd.Name().Contains("ignored", StringComparison.InvariantCultureIgnoreCase));
    }

    [Fact]
    public void Should_Not_Contain_K8s_Entities()
    {
        _crds.Should()
            .NotContain(crd => crd.Spec.Names.Kind == "Pod");
    }

    [Fact]
    public void Should_Set_Highest_Version_As_Storage()
    {
        var crd = _crds.First(c => c.Spec.Names.Kind == "VersionedEntity");
        crd.Spec.Versions.Count(v => v.Storage).Should().Be(1);
        crd.Spec.Versions.First(v => v.Storage).Name.Should().Be("v2");
    }

    [Fact]
    public void Should_Set_Storage_When_Attribute_Is_Set()
    {
        var crd = _crds.First(c => c.Spec.Names.Kind == "AttributeVersionedEntity");
        crd.Spec.Versions.Count(v => v.Storage).Should().Be(1);
        crd.Spec.Versions.First(v => v.Storage).Name.Should().Be("v1");
    }

    [Fact]
    public void Should_Add_Multiple_Versions_To_Crd()
    {
        _crds
            .First(c => c.Spec.Names.Kind.Contains("testspecentity", StringComparison.InvariantCultureIgnoreCase))
            .Spec.Versions.Should()
            .HaveCount(1);
        _crds
            .First(c => c.Spec.Names.Kind.Contains("teststatusentity", StringComparison.InvariantCultureIgnoreCase))
            .Spec.Versions.Should()
            .HaveCount(1);
        _crds
            .First(c => c.Spec.Names.Kind == "VersionedEntity")
            .Spec.Versions.Should()
            .HaveCount(5);
        _crds
            .First(c => c.Spec.Names.Kind == "AttributeVersionedEntity")
            .Spec.Versions.Should()
            .HaveCount(2);
    }

    [Fact]
    public void Should_Add_ShortNames_To_Crd()
    {
        _crds
            .First(c => c.Spec.Names.Kind.Contains("teststatusentity", StringComparison.InvariantCultureIgnoreCase))
            .Spec.Names.ShortNames.Should()
            .NotBeNull()
            .And
            .Contain(new[] { "foo", "bar", "baz" });
    }
}
