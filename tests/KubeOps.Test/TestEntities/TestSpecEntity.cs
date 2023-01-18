using System.Collections;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using k8s.Models;
using KubeOps.KubernetesClient.Entities;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;

namespace KubeOps.Test.TestEntities;

[Description("This is the Spec Class Description")]
public class TestSpecEntitySpec
{
    public string[] StringArray { get; set; } = Array.Empty<string>();

    public string[]? NullableStringArray { get; set; }

    public IEnumerable<int> EnumerableInteger { get; set; } = Array.Empty<int>();

    public IEnumerable<int?> EnumerableNullableInteger { get; set; } = Array.Empty<int?>();

    public IntegerList IntegerList { get; set; } = new();

    public HashSet<int> IntegerHashSet { get; set; } = new();

    public ISet<int> IntegerISet { get; set; } = new HashSet<int>();

    public IReadOnlySet<int> IntegerIReadOnlySet { get; set; } = new HashSet<int>();

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

    [IgnoreProperty]
    public string IgnoredProperty { get; set; } = string.Empty;

    public IEnumerable<TestItem> ComplexItemsEnumerable { get; set; } = Enumerable.Empty<TestItem>();

    public List<TestItem> ComplexItemsList { get; set; } = new();

    public IList<TestItem> ComplexItemsIList { get; set; } = Array.Empty<TestItem>();

    public IReadOnlyList<TestItem> ComplexItemsReadOnlyList { get; set; } = Array.Empty<TestItem>();

    public Collection<TestItem> ComplexItemsCollection { get; set; } = new();

    public ICollection<TestItem> ComplexItemsICollection { get; set; } = Array.Empty<TestItem>();

    public IReadOnlyCollection<TestItem> ComplexItemsReadOnlyCollection { get; set; } = Array.Empty<TestItem>();

    public TestItemList ComplexItemsDerivedList { get; set; } = new();

    public IDictionary Dictionary { get; set; } = new Dictionary<string, string>();

    public IDictionary<string, string> GenericDictionary { get; set; } = new Dictionary<string, string>();
    public IDictionary<string, string> NormalGenericDictionary { get; set; } = new Dictionary<string, string>();
    public IDictionary<string, string>? NullableGenericDictionary { get; set; } = new Dictionary<string, string>();

    public IEnumerable<KeyValuePair<string, string>> KeyValueEnumerable { get; set; } =
        new Dictionary<string, string>();

    public IEnumerable<KeyValuePair<string, string>> NormalKeyValueEnumerable { get; set; } =
        new Dictionary<string, string>();

    public IEnumerable<KeyValuePair<string, string>>? NullableKeyValueEnumerable { get; set; } =
        new Dictionary<string, string>();

    [PreserveUnknownFields]
    public object PreserveUnknownFields { get; set; } = new object();

    public IntstrIntOrString IntOrString { get; set; } = string.Empty;

    [EmbeddedResource]
    public V1ConfigMap KubernetesObject { get; set; } = new V1ConfigMap();

    public V1Pod Pod { get; set; } = new V1Pod();

    public IList<V1Pod> Pods { get; set; } = Array.Empty<V1Pod>();

    [JsonPropertyName("NameFromAttribute")]
    public string PropertyWithJsonAttribute { get; set; } = string.Empty;

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

[KubernetesEntity(Group = "kubeops.test.dev", ApiVersion = "V1")]
[EntityScope(EntityScope.Cluster)]
public class TestClusterSpecEntity : CustomKubernetesEntity<TestSpecEntitySpec>
{
}

public class TestItem
{
    public string Name { get; set; } = null!;
    public string Item { get; set; } = null!;
    public string Extra { get; set; } = null!;
}

public class TestItemList : List<TestItem>
{
}

public class IntegerList : Collection<int>
{
}
