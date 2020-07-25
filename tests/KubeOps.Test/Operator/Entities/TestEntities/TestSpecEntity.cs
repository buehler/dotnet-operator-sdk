using System;
using k8s.Models;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Entities.Annotations;
using KubeOps.Operator.Rbac;

[assembly: OperatorSpec("test-operator", ImagePullSecretName = "test-secret-name", ContainerRegistry = "some.azurecr.io")]

namespace KubeOps.Test.Operator.Entities.TestEntities
{

    [Description("This is the Spec Class Description")]
    public class TestSpecEntitySpec
    {
        public string[] StringArray { get; set; } = new string[0];

        public string[]? NullableStringArray { get; set; }

        public string NormalString { get; set; } = string.Empty;

        public string? NullableString { get; set; }

        public int NormalInt { get; set; }

        public int? NullableInt { get; set; }

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

        public enum TestSpecEnum
        {
            Value1,
            Value2,
        }
    }

    [KubernetesEntity(Group = "kubeops.test.dev", ApiVersion = "V1")]
    [EntityShortName("t")]
    public class TestSpecEntity : CustomKubernetesEntity<TestSpecEntitySpec>
    {
    }
}
