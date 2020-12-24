using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using k8s.Models;
using KubeOps.Operator.Commands.Generators;
using KubeOps.Operator.Services;
using Xunit;

namespace KubeOps.Test.Operator.Commands.Generators
{
    public class CrdGeneratorTest
    {
        private readonly IList<V1CustomResourceDefinition> _crds =
            CrdGenerator.GenerateCrds(new ResourceTypeService()).ToList();

        [Fact]
        public void Should_Generate_Correct_Number_Of_Crds()
        {
            _crds.Count.Should().Be(4);
        }

        [Fact]
        public void Should_Not_Contain_Ignored_Entities()
        {
            _crds.Should()
                .NotContain(crd => crd.Name().Contains("ignored", StringComparison.InvariantCultureIgnoreCase));
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
    }
}
