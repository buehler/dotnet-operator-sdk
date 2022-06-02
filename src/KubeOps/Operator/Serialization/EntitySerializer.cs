using System;
using k8s;

namespace KubeOps.Operator.Serialization;

internal class EntitySerializer
{
    public string Serialize(object @object, SerializerOutputFormat format = default)
        => format switch
        {
            SerializerOutputFormat.Yaml => KubernetesYaml.Serialize(@object),
            SerializerOutputFormat.Json => KubernetesJson.Serialize(@object),
            _ => throw new ArgumentOutOfRangeException(),
        };
}
