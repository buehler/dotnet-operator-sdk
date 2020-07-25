using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using k8s.Models;
using KubeOps.Operator.Commands.Generators;
using KubeOps.Operator.Entities.Extensions;
using KubeOps.Test.Operator.Entities.TestEntities;
using Xunit;

namespace KubeOps.Test.Operator.Entities
{
    public class CrdGenerationTest
    {
        private readonly Type _testSpecEntity = typeof(TestSpecEntity);
        private readonly Type _testStatusEntity = typeof(TestStatusEntity);

        [Fact]
        public void Should_Use_Correct_CRD()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);
            var ced = CustomEntityDefinitionExtensions.CreateResourceDefinition(_testSpecEntity);

            crd.Kind.Should().Be(V1CustomResourceDefinition.KubeKind);
            crd.Metadata.Name.Should().Be($"{ced.Plural}.{ced.Group}");
            crd.Spec.Names.Kind.Should().Be(ced.Kind);
            crd.Spec.Names.ListKind.Should().Be(ced.ListKind);
            crd.Spec.Names.Singular.Should().Be(ced.Singular);
            crd.Spec.Names.Plural.Should().Be(ced.Plural);
            crd.Spec.Scope.Should().Be(ced.Scope.ToString());
            crd.Spec.Names.ShortNames.Should().Equal(ced.ShortNames);
        }

        [Fact]
        public void Should_Add_Status_SubResource_If_Present()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testStatusEntity);
            crd.Spec.Versions.First().Subresources.Status.Should().NotBeNull();
        }

        [Fact]
        public void Should_Not_Add_Status_SubResource_If_Absent()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);
            crd.Spec.Versions.First().Subresources?.Status?.Should().BeNull();
        }

        [Theory]
        [InlineData("Int", "integer", "int32")]
        [InlineData("Long", "integer", "int64")]
        [InlineData("Float", "number", "float")]
        [InlineData("Double", "number", "double")]
        [InlineData("String", "string", null)]
        [InlineData("Bool", "boolean", null)]
        [InlineData("DateTime", "string", "date-time")]
        [InlineData("Enum", "string", null)]
        public void Should_Set_The_Correct_Type_And_Format_For_Types(string fieldName, string typeName, string? format)
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            specProperties.Type.Should().Be("object");

            var normalField = specProperties.Properties[$"normal{fieldName}"];
            normalField.Type.Should().Be(typeName);
            normalField.Format.Should().Be(format);
            normalField.Nullable.Should().BeNull();

            var nullableField = specProperties.Properties[$"nullable{fieldName}"];
            nullableField.Type.Should().Be(typeName);
            nullableField.Format.Should().Be(format);
            nullableField.Nullable.Should().BeTrue();
        }

        [Fact]
        public void Should_Set_The_Correct_Array_Type()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);
            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];

            var normalField = specProperties.Properties["stringArray"];
            normalField.Type.Should().Be("array");
            (normalField.Items as V1JSONSchemaProps)?.Type?.Should().Be("string");
            normalField.Nullable.Should().BeNull();

            var nullableField = specProperties.Properties["nullableStringArray"];
            nullableField.Type.Should().Be("array");
            (nullableField.Items as V1JSONSchemaProps)?.Type?.Should().Be("string");
            nullableField.Nullable.Should().BeTrue();
        }

        [Fact]
        public void Should_Set_Description_On_Class()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            specProperties.Description.Should().NotBe("");
        }

        [Fact]
        public void Should_Set_Description()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["description"];

            field.Description.Should().NotBe("");
        }

        [Fact]
        public void Should_Set_ExternalDocs()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["externalDocs"];

            field.ExternalDocs.Url.Should().NotBe("");
        }

        [Fact]
        public void Should_Set_ExternalDocs_Description()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["externalDocsWithDescription"];

            field.ExternalDocs.Description.Should().NotBe("");
        }

        [Fact]
        public void Should_Set_Item_Information()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["items"];

            field.Type.Should().Be("array");
            (field.Items as V1JSONSchemaProps)?.Type?.Should().Be("string");
            field.MaxItems.Should().Be(42);
            field.MinItems.Should().Be(13);
        }

        [Fact]
        public void Should_Set_Length_Information()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["length"];

            field.MinLength.Should().Be(2);
            field.MaxLength.Should().Be(42);
        }

        [Fact]
        public void Should_Set_MultipleOf()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["multipleOf"];

            field.MultipleOf.Should().Be(15);
        }

        [Fact]
        public void Should_Set_Pattern()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["pattern"];

            field.Pattern.Should().Be(@"/\d*/");
        }

        [Fact]
        public void Should_Set_RangeMinimum()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["rangeMinimum"];

            field.Minimum.Should().Be(15);
            field.ExclusiveMinimum.Should().BeTrue();
        }

        [Fact]
        public void Should_Set_RangeMaximum()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["rangeMaximum"];

            field.Maximum.Should().Be(15);
            field.ExclusiveMaximum.Should().BeTrue();
        }

        [Fact]
        public void Should_Set_Required()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testSpecEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            specProperties.Required.Should().Contain("required");
        }

        [Fact]
        public void Should_Set_Required_Null_If_No_Required()
        {
            var crd = EntityToCrdExtensions.CreateCrd(_testStatusEntity);

            var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
            specProperties.Required.Should().BeNull();
        }

        [Fact]
        public void Should_Ignore_Entity_With_Ignore_Attribute()
        {
            var crds = CrdGenerator.GenerateCrds(Assembly.GetExecutingAssembly()).ToList();
            crds.Should().NotContain(crd => crd.Spec.Names.Kind == "TestIgnoredEntity");
            crds.Should().Contain(crd => crd.Spec.Names.Kind == "TestSpecEntity");
        }
    }
}
