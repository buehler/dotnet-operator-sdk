using k8s;

namespace KubeOps.Operator.Serialization;

internal static class EntitySerializer
{
    public static string Serialize(object @object, SerializerOutputFormat format = default)
        => format switch
        {
            SerializerOutputFormat.Yaml => KubernetesYaml.Serialize(@object),
            SerializerOutputFormat.Json => KubernetesJson.Serialize(@object),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
}
