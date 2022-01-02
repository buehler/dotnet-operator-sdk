using System;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace KubeOps.Operator.Serialization;

internal class EntitySerializer
{
    private readonly ISerializer _yaml;
    private readonly JsonSerializerSettings _jsonSettings;

    public EntitySerializer(ISerializer yaml, OperatorSettings operatorSettings)
    {
        _yaml = yaml;
        _jsonSettings = operatorSettings.SerializerSettings;
        _jsonSettings.Formatting = Formatting.Indented;
        _jsonSettings.NullValueHandling = NullValueHandling.Ignore;
    }

    public string Serialize(object @object, SerializerOutputFormat format = default)
        => format switch
        {
            SerializerOutputFormat.Yaml => _yaml.Serialize(@object),
            SerializerOutputFormat.Json => JsonConvert.SerializeObject(@object, _jsonSettings),
            _ => throw new ArgumentOutOfRangeException(),
        };
}
