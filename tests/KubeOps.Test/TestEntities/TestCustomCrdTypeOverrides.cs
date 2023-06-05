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
    public TestResourceRequirements TestResourceRequirements { get; set; } = new();
    public DateTime LastModified { get; set; }
    public V1ResourceRequirements AnotherResourceRequirements { get; set; } = new();
}

[KubernetesEntity(
    ApiVersion = "v1",
    Kind = "TestCustomTypeOverrides",
    Group = "kubeops.test.dev",
    PluralName = "testcustomtypeoverrides")]
public class TestCustomCrdTypeOverrides : CustomKubernetesEntity<TestSpec, TestStatus>
{
}

public static class TestTypeOverridesValues
{
    public const string ExpectedOverriddenResourcesYaml = @"
                      limits:
                        description: |-
                          Limits describes the maximum amount of compute resources allowed. More info:
                          https://kubernetes.io/docs/concepts/configuration/manage-resources-containers/
                        type: object
                        x-kubernetes-preserve-unknown-fields: true
                      requests:
                        description: |-
                          Requests describes the minimum amount of compute resources required. If Requests
                          is omitted for a container, it defaults to Limits if that is explicitly
                          specified, otherwise to an implementation-defined value. More info:
                          https://kubernetes.io/docs/concepts/configuration/manage-resources-containers/
                        type: object
                        x-kubernetes-preserve-unknown-fields: true";

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
