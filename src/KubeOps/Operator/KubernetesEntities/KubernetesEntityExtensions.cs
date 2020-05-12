using System.Collections.Generic;
using System.Linq;
using System.Text;
using k8s.Models;

namespace KubeOps.Operator.KubernetesEntities
{
    public static class KubernetesEntityExtensions
    {
        public static string ReadData(this V1Secret secret, string key)
            => Encoding.UTF8.GetString(secret.Data[key]);

        public static void WriteData(this V1Secret secret, string key, string value)
            => secret.Data[key] = Encoding.UTF8.GetBytes(value);

        internal static V1beta1CustomResourceDefinition Convert(this V1CustomResourceDefinition crd)
        {
            var crdVersion = crd.Spec.Versions.First();
            var betaCrd = new V1beta1CustomResourceDefinition(
                new V1beta1CustomResourceDefinitionSpec(),
                $"{V1beta1CustomResourceDefinition.KubeGroup}/{V1beta1CustomResourceDefinition.KubeApiVersion}",
                V1beta1CustomResourceDefinition.KubeKind,
                crd.Metadata);

            betaCrd.Spec.Group = crd.Spec.Group;
            betaCrd.Spec.Names = new V1beta1CustomResourceDefinitionNames
            {
                Kind = crd.Spec.Names.Kind,
                ListKind = crd.Spec.Names.ListKind,
                Singular = crd.Spec.Names.Singular,
                Plural = crd.Spec.Names.Plural,
            };
            betaCrd.Spec.Scope = crd.Spec.Scope;
            betaCrd.Spec.Version = crdVersion.Name;
            betaCrd.Spec.Versions = new List<V1beta1CustomResourceDefinitionVersion>
            {
                new V1beta1CustomResourceDefinitionVersion(crdVersion.Name, true, true),
            };

            if (crdVersion.Subresources != null)
            {
                betaCrd.Spec.Subresources = new V1beta1CustomResourceSubresources(null, new { });
            }

            betaCrd.Spec.Validation = new V1beta1CustomResourceValidation(crdVersion.Schema.OpenAPIV3Schema.Convert());

            return betaCrd;
        }

        private static V1beta1JSONSchemaProps Convert(this V1JSONSchemaProps props)
        {
            var betaProps = new V1beta1JSONSchemaProps();

            betaProps.Description = props.Description;
            betaProps.Type = props.Type;
            betaProps.Format = props.Format;
            betaProps.EnumProperty = props.EnumProperty;
            betaProps.Nullable = props.Nullable;
            if (props.Properties != null)
            {
                betaProps.Properties = new Dictionary<string, V1beta1JSONSchemaProps>(
                    props.Properties.Select(p => KeyValuePair.Create(p.Key, p.Value.Convert())));
            }

            return betaProps;
        }
    }
}
