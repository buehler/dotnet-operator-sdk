using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace KubeOps.Operator.Serialization
{
    internal class YamlByteArrayTypeConverter : IYamlTypeConverter
    {
        private static readonly Type AcceptType = typeof(byte[]);

        public bool Accepts(Type type) => type == AcceptType;

        public object ReadYaml(IParser parser, Type type)
        {
            var str = parser.Consume<Scalar>().Value;
            return Convert.FromBase64String(str);
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type)
        {
            var str = value is byte[] bytes ? Convert.ToBase64String(bytes) : string.Empty;
            emitter.Emit(new Scalar(str));
        }
    }
}
