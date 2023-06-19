using System.IdentityModel.Tokens.Jwt;
using System.Text;
using k8s;
using k8s.Models;
using KubeOps.Operator.Entities;
using YamlDotNet.RepresentationModel;

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
    public static string SerializeWithoutDescriptions(V1CustomResourceDefinition? resource)
    {
        var yamlText = KubernetesYaml.Serialize(resource);
        var yaml = new YamlStream();
        yaml.Load(new StringReader(yamlText));

        var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        RemoveDescriptionFromYaml(mapping);

        var stringWriter = new StringWriter();
        yaml.Save(stringWriter, false);

        var updatedYamlText = stringWriter.ToString();
        return updatedYamlText;

    }

    /// <summary>
    /// Recursively removes all yaml keys value pairs named "description".
    /// </summary>
    /// <param name="node"></param>
    private static void RemoveDescriptionFromYaml(YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                {
                    var nodesToRemove = new List<YamlNode>();

                    foreach (var entry in mapping.Children)
                    {
                        if (entry.Key.ToString() == "description")
                        {
                            nodesToRemove.Add(entry.Key);
                        }
                        else
                        {
                            RemoveDescriptionFromYaml(entry.Value);
                        }
                    }

                    foreach (var key in nodesToRemove)
                    {
                        mapping.Children.Remove(key);
                    }

                    break;
                }
            case YamlSequenceNode sequence:
                {
                    foreach (var child in sequence.Children)
                    {
                        RemoveDescriptionFromYaml(child);
                    }

                    break;
                }
        }
    }


    public const string ExpectedOverriddenResourcesYaml = @"
                      limits:
                        type: object
                        x-kubernetes-preserve-unknown-fields: true
                      requests:
                        type: object
                        x-kubernetes-preserve-unknown-fields: true";

    public const string ExpectedDefaultYamlResources = @"
                  resources:
                    properties:
                      claims:
                        items:
                          properties:
                            name:
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
                        type: object";
}
