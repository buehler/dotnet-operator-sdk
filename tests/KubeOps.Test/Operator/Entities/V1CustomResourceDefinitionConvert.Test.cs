using System;
using FluentAssertions;
using k8s.Models;
using KubeOps.Operator.Entities.Extensions;
using KubeOps.Test.TestEntities;
using Xunit;

namespace KubeOps.Test.Operator.Entities
{
    public class V1CustomResourceDefinitionConvertTest
    {
        private readonly Type _testSpecEntity = typeof(TestSpecEntity);
        private readonly Type _testStatusEntity = typeof(TestStatusEntity);

        [Fact]
        public void Should_Correctly_Translate_CRD()
        {
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();
            var ced = _testSpecEntity.CreateResourceDefinition();

            crd.Kind.Should().Be(V1CustomResourceDefinition.KubeKind);
            crd.Metadata.Name.Should().Be($"{ced.Plural}.{ced.Group}");
            crd.Spec.Names.Kind.Should().Be(ced.Kind);
            crd.Spec.Names.ListKind.Should().Be(ced.ListKind);
            crd.Spec.Names.Singular.Should().Be(ced.Singular);
            crd.Spec.Names.Plural.Should().Be(ced.Plural);
            crd.Spec.Scope.Should().Be(ced.Scope.ToString());
        }

        [Fact]
        public void Should_Add_Status_SubResource_If_Present()
        {
            var crd = (V1beta1CustomResourceDefinition)_testStatusEntity.CreateCrd();
            crd.Spec.Subresources.Status.Should().NotBeNull();
        }

        [Fact]
        public void Should_Not_Add_Status_SubResource_If_Absent()
        {
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();
            crd.Spec.Subresources?.Status?.Should().BeNull();
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
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
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
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();
            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];

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
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
            specProperties.Description.Should().NotBe("");
        }

        [Fact]
        public void Should_Set_Description()
        {
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["description"];

            field.Description.Should().NotBe("");
        }

        [Fact]
        public void Should_Set_ExternalDocs()
        {
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["externalDocs"];

            field.ExternalDocs.Url.Should().NotBe("");
        }

        [Fact]
        public void Should_Set_ExternalDocs_Description()
        {
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["externalDocsWithDescription"];

            field.ExternalDocs.Description.Should().NotBe("");
        }

        [Fact]
        public void Should_Set_Item_Information()
        {
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["items"];

            field.Type.Should().Be("array");
            (field.Items as V1JSONSchemaProps)?.Type?.Should().Be("string");
            field.MaxItems.Should().Be(42);
            field.MinItems.Should().Be(13);
        }

        [Fact]
        public void Should_Set_Length_Information()
        {
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["length"];

            field.MinLength.Should().Be(2);
            field.MaxLength.Should().Be(42);
        }

        [Fact]
        public void Should_Set_MultipleOf()
        {
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["multipleOf"];

            field.MultipleOf.Should().Be(15);
        }

        [Fact]
        public void Should_Set_Pattern()
        {
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["pattern"];

            field.Pattern.Should().Be(@"/\d*/");
        }

        [Fact]
        public void Should_Set_RangeMinimum()
        {
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["rangeMinimum"];

            field.Minimum.Should().Be(15);
            field.ExclusiveMinimum.Should().BeTrue();
        }

        [Fact]
        public void Should_Set_RangeMaximum()
        {
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
            var field = specProperties.Properties["rangeMaximum"];

            field.Maximum.Should().Be(15);
            field.ExclusiveMaximum.Should().BeTrue();
        }

        [Fact]
        public void Should_Set_Required()
        {
            var crd = (V1beta1CustomResourceDefinition)_testSpecEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
            specProperties.Required.Should().Contain("required");
        }

        [Fact]
        public void Should_Set_Required_Null_If_No_Required()
        {
            var crd = (V1beta1CustomResourceDefinition)_testStatusEntity.CreateCrd();

            var specProperties = crd.Spec.Validation.OpenAPIV3Schema.Properties["spec"];
            specProperties.Required.Should().BeNull();
        }
    }
}
