using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;

namespace KubeOps.Test.TestEntities
{
    [Description("This is the Spec Class Description")]
    public class TestSpecEntitySpec
    {
        public string[] StringArray { get; set; } = new string[0];

        public string[]? NullableStringArray { get; set; }

        [AdditionalPrinterColumn]
        public string NormalString { get; set; } = string.Empty;

        public string? NullableString { get; set; }

        [AdditionalPrinterColumn(Priority = 1)]
        public int NormalInt { get; set; }

        public int? NullableInt { get; set; }

        [AdditionalPrinterColumn(Name = "OtherName")]
        public long NormalLong { get; set; }

        public long? NullableLong { get; set; }

        public float NormalFloat { get; set; }

        public float? NullableFloat { get; set; }

        public double NormalDouble { get; set; }

        public double? NullableDouble { get; set; }

        public bool NormalBool { get; set; }

        public bool? NullableBool { get; set; }

        public DateTime NormalDateTime { get; set; }

        public DateTime? NullableDateTime { get; set; }

        public TestSpecEnum NormalEnum { get; set; }

        public TestSpecEnum? NullableEnum { get; set; }

        [Description("Description")]
        public string Description { get; set; } = string.Empty;

        [ExternalDocs("https://google.ch")]
        public string ExternalDocs { get; set; } = string.Empty;

        [ExternalDocs("https://google.ch", "Description")]
        public string ExternalDocsWithDescription { get; set; } = string.Empty;

        [Items(MaxItems = 42, MinItems = 13)]
        public string[] Items { get; set; } = new string[0];

        [Length(MinLength = 2, MaxLength = 42)]
        public string Length { get; set; } = string.Empty;

        [MultipleOf(15)]
        public int MultipleOf { get; set; }

        [Pattern(@"/\d*/")]
        public string Pattern { get; set; } = string.Empty;

        [RangeMinimum(Minimum = 15, ExclusiveMinimum = true)]
        public int RangeMinimum { get; set; }

        [RangeMaximum(Maximum = 15, ExclusiveMaximum = true)]
        public int RangeMaximum { get; set; }

        [Required]
        public int Required { get; set; }

        public IEnumerable<TestItem> ComplexItems { get; set; } = Enumerable.Empty<TestItem>();

        public IDictionary Dictionary { get; set; } = new Dictionary<string, string>();

        public IDictionary<string, string> GenericDictionary { get; set; } = new Dictionary<string, string>();

        public IEnumerable<KeyValuePair<string, string>> KeyValueEnumerable { get; set; } =
            new Dictionary<string, string>();

        [PreserveUnknownFields]
        public object PreserveUnknownFields { get; set; } = new object();

        public IntstrIntOrString IntOrString { get; set; } = string.Empty;

        [EmbeddedResource]
        public V1ConfigMap KubernetesObject { get; set; } = new V1ConfigMap();

        public enum TestSpecEnum
        {
            Value1,
            Value2,
        }
    }

    [KubernetesEntity(Group = "kubeops.test.dev", ApiVersion = "V1")]
    [GenericAdditionalPrinterColumn(".metadata.namespace", "Namespace", "string")]
    [GenericAdditionalPrinterColumn(".metadata.creationTimestamp", "Age", "date")]
    public class TestSpecEntity : CustomKubernetesEntity<TestSpecEntitySpec>
    {
    }

    public class TestItem
    {
        public string Name { get; set; } = null!;
        public string Item { get; set; } = null!;
        public string Extra { get; set; } = null!;
    }
}
