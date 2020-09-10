using System;
using k8s.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace KubeOps.Operator.Serialization
{
    internal class YamlIntOrStrTypeConverter : IYamlTypeConverter
    {
        private static readonly Type AcceptType = typeof(IntstrIntOrString);

        public bool Accepts(Type type) => type == AcceptType;

        public object? ReadYaml(IParser parser, Type type) => (IntstrIntOrString)parser.Consume<Scalar>().Value;

        public void WriteYaml(IEmitter emitter, object? value, Type type)
        {
            var str = value is IntstrIntOrString intstrIntOrString ? intstrIntOrString.Value : string.Empty;
            emitter.Emit(new Scalar(str));
        }
    }
}
