using System.Collections.Generic;
using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace KubeOps.Operator.KubernetesEntities
{
    public class V1JSONSchemaProps : k8s.Models.V1JSONSchemaProps
    {
        [JsonProperty(PropertyName = "enum")]
        [YamlMember(Alias = "enum")]
        public new IList<object>? EnumProperty { get; set; }
    }
}
