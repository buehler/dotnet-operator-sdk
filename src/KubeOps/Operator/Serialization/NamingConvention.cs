using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KubeOps.Operator.Serialization
{
    internal class NamingConvention : CamelCasePropertyNamesContractResolver, INamingConvention
    {
        private readonly INamingConvention _yamlNaming = CamelCaseNamingConvention.Instance;

        private readonly IDictionary<string, string> _rename = new Dictionary<string, string>
        {
            {"namespaceProperty", "namespace"},
            {"enumProperty", "enum"},
            {"objectProperty", "object"},
        };

        public string Apply(string value)
        {
            var (key, renamedValue) = _rename.FirstOrDefault(p =>
                string.Equals(value, p.Key, StringComparison.InvariantCultureIgnoreCase));

            if (key != default)
            {
                value = renamedValue;
            }

            return _yamlNaming.Apply(value);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            var (key, renamedValue) = _rename.FirstOrDefault(p =>
                string.Equals(property.PropertyName, p.Key, StringComparison.InvariantCultureIgnoreCase));

            if (key != default)
            {
                property.PropertyName = renamedValue;
            }

            return property;
        }
    }
}
