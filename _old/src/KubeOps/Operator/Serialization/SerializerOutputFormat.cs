namespace KubeOps.Operator.Serialization;

internal enum SerializerOutputFormat
{
    /// <summary>
    /// Return the generated output in yaml format.
    /// </summary>
    Yaml,

    /// <summary>
    /// Return the generated output in json format.
    /// </summary>
    Json,
}
