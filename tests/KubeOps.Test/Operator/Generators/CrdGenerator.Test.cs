using System.Reflection;
using FluentAssertions;
using k8s;
using k8s.Models;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Entities;
using KubeOps.Test.TestEntities;
using Xunit;

namespace KubeOps.Test.Operator.Generators;

public class CrdGeneratorTest
{
    private readonly IEnumerable<V1CustomResourceDefinition> _crds;
    private readonly ComponentRegistrar _componentRegistrar = new();

    public CrdGeneratorTest()
    {
        _componentRegistrar.RegisterEntity<TestIgnoredEntity>();
        _componentRegistrar.RegisterEntity<TestInvalidEntity>();
        _componentRegistrar.RegisterEntity<TestSpecEntity>();
        _componentRegistrar.RegisterEntity<TestClusterSpecEntity>();
        _componentRegistrar.RegisterEntity<TestStatusEntity>();
        _componentRegistrar.RegisterEntity<V1Alpha1VersionedEntity>();
        _componentRegistrar.RegisterEntity<V1AttributeVersionedEntity>();
        _componentRegistrar.RegisterEntity<V1Beta1VersionedEntity>();
        _componentRegistrar.RegisterEntity<V1VersionedEntity>();
        _componentRegistrar.RegisterEntity<V2AttributeVersionedEntity>();
        _componentRegistrar.RegisterEntity<V2Beta2VersionedEntity>();
        _componentRegistrar.RegisterEntity<V2VersionedEntity>();
        _componentRegistrar.RegisterEntity<TestCustomCrdTypeOverrides>();

        // Should be ignored since V1Pod is from the k8s assembly.
        _componentRegistrar.RegisterEntity<V1Pod>();

        _crds = new CrdBuilder(_componentRegistrar).BuildCrds();
    }

    [Fact]
    public void Should_Generate_Correct_Number_Of_Crds()
    {
        _crds.Count().Should().Be(6);
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

    [Fact]
    public void Should_Create_Crd_As_Default_Without_Crd_Type_Overrides()
    {
        var crdWithoutOverrides = new CrdBuilder(_componentRegistrar)
            .BuildCrds()
            .First(

                c => c.Spec.Names.Kind.Contains("testcustomtypeoverrides", StringComparison.InvariantCultureIgnoreCase));


        var serializedWithoutOverrides = TestTypeOverridesValues.SerializeWithoutDescriptions(crdWithoutOverrides);

        serializedWithoutOverrides.Should().Contain(TestTypeOverridesValues.ExpectedDefaultYamlResources);
        serializedWithoutOverrides.Should().NotContain(TestTypeOverridesValues.ExpectedOverriddenResourcesYaml);
    }

    [Fact]
    public void Should_Convert_Desired_Crd_Type_Everywhere_To_Desired_Crd_Format()
    {
        var customOverrides = new List<ICrdBuilderTypeOverride> { new CrdBuilderResourceQuantityOverride() };
        var crdWithTypeOverrides = new CrdBuilder(_componentRegistrar, customOverrides)
            .BuildCrds()
            .First(
                c => c.Spec.Names.Kind.Contains("testcustomtypeoverrides", StringComparison.InvariantCultureIgnoreCase));
        var serializedWithOverrides = TestTypeOverridesValues.SerializeWithoutDescriptions(crdWithTypeOverrides);

        serializedWithOverrides.Should().Contain(TestTypeOverridesValues.ExpectedOverriddenResourcesYaml);
        serializedWithOverrides.Should().NotContain(TestTypeOverridesValues.ExpectedDefaultYamlResources);

    }
}
