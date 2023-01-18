using System.Reflection;
using System.Text.Json.Serialization;
using FluentAssertions;
using k8s.Models;
using KubeOps.KubernetesClient.Entities;
using KubeOps.Operator.Entities.Extensions;
using KubeOps.Operator.Errors;
using KubeOps.Operator.Util;
using KubeOps.Test.TestEntities;
using Xunit;

namespace KubeOps.Test.Operator.Entities;

public class CrdGenerationTest
{
    private readonly Type _testSpecEntity = typeof(TestSpecEntity);
    private readonly Type _testClusterSpecEntity = typeof(TestClusterSpecEntity);
    private readonly Type _testStatusEntity = typeof(TestStatusEntity);

    [Fact]
    public void Should_Use_Correct_CRD()
    {
        var crd = _testSpecEntity.CreateCrd();
        var ced = _testSpecEntity.ToEntityDefinition();

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
        var crd = _testStatusEntity.CreateCrd();
        crd.Spec.Versions.First().Subresources.Status.Should().NotBeNull();
    }

    [Fact]
    public void Should_Not_Add_Status_SubResource_If_Absent()
    {
        var crd = _testSpecEntity.CreateCrd();
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
    [InlineData(nameof(TestSpecEntitySpec.GenericDictionary), "object", null)]
    [InlineData(nameof(TestSpecEntitySpec.KeyValueEnumerable), "object", null)]
    public void Should_Set_The_Correct_Type_And_Format_For_Types(string fieldName, string typeName, string? format)
    {
        var crd = _testSpecEntity.CreateCrd();

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

    [Theory]
    [InlineData(nameof(TestSpecEntitySpec.StringArray), "string", null)]
    [InlineData(nameof(TestSpecEntitySpec.NullableStringArray), "string", true)]
    [InlineData(nameof(TestSpecEntitySpec.EnumerableInteger), "integer", null)]
    [InlineData(nameof(TestSpecEntitySpec.EnumerableNullableInteger), "integer", null)]
    [InlineData(nameof(TestSpecEntitySpec.IntegerList), "integer", null)]
    [InlineData(nameof(TestSpecEntitySpec.IntegerHashSet), "integer", null)]
    [InlineData(nameof(TestSpecEntitySpec.IntegerISet), "integer", null)]
    [InlineData(nameof(TestSpecEntitySpec.IntegerIReadOnlySet), "integer", null)]
    public void Should_Set_The_Correct_Array_Type(string property, string expectedType, bool? expectedNullable)
    {
        var propertyName = property.ToCamelCase();
        var crd = _testSpecEntity.CreateCrd();
        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];

        var normalField = specProperties.Properties[propertyName];
        normalField.Type.Should().Be("array");
        (normalField.Items as V1JSONSchemaProps)?.Type?.Should().Be(expectedType);
        normalField.Nullable.Should().Be(expectedNullable);
    }

    [Theory]
    [InlineData(nameof(TestSpecEntitySpec.ComplexItemsEnumerable))]
    [InlineData(nameof(TestSpecEntitySpec.ComplexItemsList))]
    [InlineData(nameof(TestSpecEntitySpec.ComplexItemsIList))]
    [InlineData(nameof(TestSpecEntitySpec.ComplexItemsReadOnlyList))]
    [InlineData(nameof(TestSpecEntitySpec.ComplexItemsCollection))]
    [InlineData(nameof(TestSpecEntitySpec.ComplexItemsICollection))]
    [InlineData(nameof(TestSpecEntitySpec.ComplexItemsReadOnlyCollection))]
    [InlineData(nameof(TestSpecEntitySpec.ComplexItemsDerivedList))]
    public void Should_Set_The_Correct_Complex_Array_Type(string property)
    {
        var propertyName = property.ToCamelCase();
        var crd = _testSpecEntity.CreateCrd();
        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];

        var complexItemsArray = specProperties.Properties[propertyName];
        complexItemsArray.Type.Should().Be("array");
        (complexItemsArray.Items as V1JSONSchemaProps)?.Type?.Should().Be("object");
        complexItemsArray.Nullable.Should().BeNull();
        var subProps = (complexItemsArray.Items as V1JSONSchemaProps)!.Properties;

        var subName = subProps["name"];
        subName?.Type.Should().Be("string");
    }

    [Fact]
    public void Should_Set_Description_On_Class()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Description.Should().NotBe("");
    }

    [Fact]
    public void Should_Set_Description()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        var field = specProperties.Properties["description"];

        field.Description.Should().NotBe("");
    }

    [Fact]
    public void Should_Set_ExternalDocs()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        var field = specProperties.Properties["externalDocs"];

        field.ExternalDocs.Url.Should().NotBe("");
    }

    [Fact]
    public void Should_Set_ExternalDocs_Description()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        var field = specProperties.Properties["externalDocsWithDescription"];

        field.ExternalDocs.Description.Should().NotBe("");
    }

    [Fact]
    public void Should_Set_Item_Information()
    {
        var crd = _testSpecEntity.CreateCrd();

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
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        var field = specProperties.Properties["length"];

        field.MinLength.Should().Be(2);
        field.MaxLength.Should().Be(42);
    }

    [Fact]
    public void Should_Set_MultipleOf()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        var field = specProperties.Properties["multipleOf"];

        field.MultipleOf.Should().Be(15);
    }

    [Fact]
    public void Should_Set_Pattern()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        var field = specProperties.Properties["pattern"];

        field.Pattern.Should().Be(@"/\d*/");
    }

    [Fact]
    public void Should_Set_RangeMinimum()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        var field = specProperties.Properties["rangeMinimum"];

        field.Minimum.Should().Be(15);
        field.ExclusiveMinimum.Should().BeTrue();
    }

    [Fact]
    public void Should_Set_RangeMaximum()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        var field = specProperties.Properties["rangeMaximum"];

        field.Maximum.Should().Be(15);
        field.ExclusiveMaximum.Should().BeTrue();
    }

    [Fact]
    public void Should_Set_Required()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Required.Should().Contain("required");
    }

    [Fact]
    public void Should_Set_Required_Null_If_No_Required()
    {
        var crd = _testStatusEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Required.Should().BeNull();
    }

    [Fact]
    public void Should_Set_Preserve_Unknown_Fields()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Properties["preserveUnknownFields"].XKubernetesPreserveUnknownFields.Should().BeTrue();
    }

    [Fact]
    public void Should_Set_Preserve_Unknown_Fields_On_Dictionaries()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Properties["dictionary"].XKubernetesPreserveUnknownFields.Should().BeTrue();
    }

    [Fact]
    public void Should_Not_Set_Preserve_Unknown_Fields_On_Generic_Dictionaries()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Properties["genericDictionary"].XKubernetesPreserveUnknownFields.Should().BeNull();
    }

    [Fact]
    public void Should_Not_Set_Preserve_Unknown_Fields_On_KeyValuePair_Enumerable()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Properties["keyValueEnumerable"].XKubernetesPreserveUnknownFields.Should().BeNull();
    }

    [Fact]
    public void Should_Not_Set_Properties_On_Dictionaries()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Properties["dictionary"].Properties.Should().BeNull();
    }

    [Fact]
    public void Should_Not_Set_Properties_On_Generic_Dictionaries()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Properties["genericDictionary"].Properties.Should().BeNull();
    }

    [Fact]
    public void Should_Not_Set_Properties_On_KeyValuePair_Enumerable()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Properties["keyValueEnumerable"].Properties.Should().BeNull();
    }

    [Fact]
    public void Should_Set_AdditionalProperties_On_Dictionaries_For_Value_type()
    {
        const string propertyName = nameof(TestSpecEntity.Spec.GenericDictionary);
        var valueType = _testSpecEntity
            .GetProperty(nameof(TestSpecEntity.Spec))!
            .PropertyType.GetProperty(propertyName)!
            .PropertyType.GetGenericArguments()[1]
            .Name;

        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        var valueItems =
            specProperties.Properties[propertyName.ToCamelCase()].AdditionalProperties as V1JSONSchemaProps;
        valueItems.Should().NotBeNull();
        valueItems!.Type.Should().Be(valueType.ToCamelCase());
    }

    [Fact]
    public void Should_Set_AdditionalProperties_On_KeyValuePair_For_Value_type()
    {
        const string propertyName = nameof(TestSpecEntity.Spec.KeyValueEnumerable);
        var valueType = _testSpecEntity
            .GetProperty(nameof(TestSpecEntity.Spec))!
            .PropertyType.GetProperty(propertyName)!
            .PropertyType.GetGenericArguments()[0]
            .GetGenericArguments()[1]
            .Name;

        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        var valueItems =
            specProperties.Properties[propertyName.ToCamelCase()].AdditionalProperties as V1JSONSchemaProps;
        valueItems.Should().NotBeNull();
        valueItems!.Type.Should().Be(valueType.ToCamelCase());
    }

    [Fact]
    public void Should_Set_IntOrString()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Properties["intOrString"].Properties.Should().BeNull();
        specProperties.Properties["intOrString"].XKubernetesIntOrString.Should().BeTrue();
    }

    [Theory]
    [InlineData(nameof(TestSpecEntitySpec.KubernetesObject))]
    [InlineData(nameof(TestSpecEntitySpec.Pod))]
    public void Should_Map_Embedded_Resources(string property)
    {
        var crd = _testSpecEntity.CreateCrd();
        var propertyName = property.ToCamelCase();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Properties[propertyName].Type.Should().Be("object");
        specProperties.Properties[propertyName].Properties.Should().BeNull();
        specProperties.Properties[propertyName].XKubernetesPreserveUnknownFields.Should().BeTrue();
        specProperties.Properties[propertyName].XKubernetesEmbeddedResource.Should().BeTrue();
    }

    [Fact]
    public void Should_Map_List_Of_Embedded_Resource()
    {
        var crd = _testSpecEntity.CreateCrd();
        var propertyName = nameof(TestSpecEntitySpec.Pods).ToCamelCase();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        var arrayProperty = specProperties.Properties[propertyName];
        arrayProperty.Type.Should().Be("array");

        var items = arrayProperty.Items as V1JSONSchemaProps;
        items?.Type?.Should().Be("object");
        items?.XKubernetesPreserveUnknownFields.Should().BeTrue();
        items?.XKubernetesEmbeddedResource?.Should().BeTrue();
    }

    [Fact]
    public void Should_Throw_On_Not_Supported_Type()
    {
        var ex = Assert.Throws<CrdConversionException>(() => typeof(TestInvalidEntity).CreateCrd());
        ex.InnerException.Should().BeOfType<CrdPropertyTypeException>();
    }

    [Fact]
    public void Should_Use_PropertyName_From_JsonPropertyAttribute()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        const string propertyNameFromType = nameof(TestSpecEntitySpec.PropertyWithJsonAttribute);
        var propertyNameFromAttribute = typeof(TestSpecEntitySpec)
            .GetProperty(propertyNameFromType)
            ?.GetCustomAttribute<JsonPropertyNameAttribute>()
            ?.Name;
        specProperties.Properties.Should().ContainKey(propertyNameFromAttribute?.ToCamelCase());
        specProperties.Properties.Should().NotContainKey(propertyNameFromType.ToCamelCase());
    }

    [Fact]
    public void Should_Add_AdditionalPrinterColumns()
    {
        var crd = _testSpecEntity.CreateCrd();
        var apc = crd.Spec.Versions.First().AdditionalPrinterColumns;

        apc.Should().NotBeNull();
        apc.Should()
            .ContainSingle(
                def => def.JsonPath == ".spec.normalString" && def.Name == "NormalString" && def.Priority == 0);
        apc.Should().ContainSingle(def => def.JsonPath == ".spec.normalInt" && def.Priority == 1);
        apc.Should().ContainSingle(def => def.JsonPath == ".spec.normalLong" && def.Name == "OtherName");
    }

    [Fact]
    public void Must_Not_Contain_Ignored_TopLevel_Properties()
    {
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties;
        specProperties.Should().NotContainKeys("metadata", "apiVersion", "kind");
    }

    [Fact]
    public void Should_Add_GenericAdditionalPrinterColumns()
    {
        var crd = _testSpecEntity.CreateCrd();
        var apc = crd.Spec.Versions.First().AdditionalPrinterColumns;

        apc.Should().NotBeNull();
        apc.Should().ContainSingle(def => def.JsonPath == ".metadata.creationTimestamp" && def.Name == "Age");
    }

    [Fact]
    public void Should_Correctly_Use_Entity_Scope_Attribute()
    {
        var scopedCrd = _testSpecEntity.CreateCrd();
        var clusterCrd = _testClusterSpecEntity.CreateCrd();

        scopedCrd.Spec.Scope.Should().Be("Namespaced");
        clusterCrd.Spec.Scope.Should().Be("Cluster");
    }

    [Fact]
    public void Should_Not_Contain_Ignored_Property()
    {
        const string propertyName = nameof(TestSpecEntity.Spec.IgnoredProperty);
        var crd = _testSpecEntity.CreateCrd();

        var specProperties = crd.Spec.Versions.First().Schema.OpenAPIV3Schema.Properties["spec"];
        specProperties.Properties.Should().NotContainKey(propertyName.ToCamelCase());
    }
}
