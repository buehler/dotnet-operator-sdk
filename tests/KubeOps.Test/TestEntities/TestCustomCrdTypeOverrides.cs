using k8s.Models;
using KubeOps.Operator.Entities;

namespace KubeOps.Test.TestEntities;

public class TestStatus
{
    public string SpecString { get; set; } = string.Empty;
}

public class TestResourceRequirements
{
    public V1ResourceRequirements Resources { get; set; } = new();
}

public class TestSpec
{
    public TestResourceRequirements ObjectName { get; set; } = new();
    public DateTime LastModified { get; set; }
}

[KubernetesEntity(
    ApiVersion = "v1",
    Kind = "TestCustomTypeOverrides",
    Group = "kubeops.test.dev",
    PluralName = "testcustomtypeoverrides")]
public class TestCustomCrdTypeOverrides : CustomKubernetesEntity<TestSpec, TestStatus>
{
}

public class TestTypeOverride : ICrdBuilderTypeOverride
{

    public bool TypeMatchesOverrideCondition(Type type) => type.IsGenericType
                                                           && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                                                           && type.GenericTypeArguments.Contains(typeof(ResourceQuantity));
    public void ConfigureCustomSchemaForProp(V1JSONSchemaProps props)
    {
        props.Type = "object";
        props.XKubernetesPreserveUnknownFields = true;
        props.Description = "";
    }

    public string? TargetJsonPath => null; // apply to everything
}

public class TestTypeOverrideWithJsonpath : ICrdBuilderTypeOverride
{
    public TestTypeOverrideWithJsonpath(string jsonpath)
    {
        TargetJsonPath = jsonpath;
    }
    public bool TypeMatchesOverrideCondition(Type type) => type.IsGenericType
                                                           && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                                                           && type.GenericTypeArguments.Contains(typeof(ResourceQuantity));
    public void ConfigureCustomSchemaForProp(V1JSONSchemaProps props)
    {
        props.Type = "test-with-jsonpath";
        props.Description = "";

        if (TargetJsonPath == "")
        {
            props.Properties = new Dictionary<string, V1JSONSchemaProps>
            {
                ["test-full-schema-control"] = new () { Type = "test-nested-property" }
            };
        }

    }

    public string TargetJsonPath { get; }
}

public static class TestTypeOverridesValues
{
    public const string ExpectedOverriddenResourcesYaml = @"
                      limits:
                        description: """"
                        type: object
                        x-kubernetes-preserve-unknown-fields: true
                      requests:
                        description: """"
                        type: object
                        x-kubernetes-preserve-unknown-fields: true";

    public const string ExpectedOverriddenResourcesWithJsonPathYaml = @"
                      limits:
                        description: """"
                        type: test-with-jsonpath
                      requests:
                        description: """"
                        type: object
                        x-kubernetes-preserve-unknown-fields: true";

    public const string ExpectedOverriddenResourcesWithMultipleJsonPathsYaml = @"
                      claims:
                        description: """"
                        type: test-with-jsonpath
                      limits:
                        description: """"
                        type: test-with-jsonpath
                      requests:
                        description: """"
                        type: object
                        x-kubernetes-preserve-unknown-fields: true
                    type: object";

    public const string ExpectedWholeControlledSchemaWithJsonPath = @"
  - name: v1
    schema:
      openAPIV3Schema:
        description: """"
        properties:
          test-full-schema-control:
            type: test-nested-property
        type: test-with-jsonpath
";

    public const string ExpectedDefaultYamlResources = @"
                  resources:
                    properties:
                      claims:
                        description: |-
                          Claims lists the names of resources, defined in spec.resourceClaims, that are
                          used by this container.

                          This is an alpha field and requires enabling the DynamicResourceAllocation
                          feature gate.

                          This field is immutable.
                        items:
                          properties:
                            name:
                              description: |-
                                Name must match the name of one entry in pod.spec.resourceClaims of the Pod
                                where this field is used. It makes that resource available inside a container.
                              type: string
                          type: object
                        type: array
                      limits:
                        additionalProperties:
                          properties:
                            format:
                              enum:
                              - DecimalExponent
                              - BinarySI
                              - DecimalSI
                              type: string
                            value:
                              type: string
                          type: object
                        description: |-
                          Limits describes the maximum amount of compute resources allowed. More info:
                          https://kubernetes.io/docs/concepts/configuration/manage-resources-containers/
                        type: object
                      requests:
                        additionalProperties:
                          properties:
                            format:
                              enum:
                              - DecimalExponent
                              - BinarySI
                              - DecimalSI
                              type: string
                            value:
                              type: string
                          type: object
                        description: |-
                          Requests describes the minimum amount of compute resources required. If Requests
                          is omitted for a container, it defaults to Limits if that is explicitly
                          specified, otherwise to an implementation-defined value. More info:
                          https://kubernetes.io/docs/concepts/configuration/manage-resources-containers/
                        type: object";
}
