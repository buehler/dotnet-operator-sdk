using System.Collections.Generic;
using System.Linq;
using k8s.Models;

namespace KubeOps.Operator.Entities.Extensions
{
    public static class V1CustomResourceDefinitionExtensions
    {
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

            betaProps.Nullable = props.Nullable;
            betaProps.Description = props.Description;

            if (props.ExternalDocs != null)
            {
                betaProps.ExternalDocs = new V1beta1ExternalDocumentation(
                    props.ExternalDocs.Description,
                    props.ExternalDocs.Url);
            }

            betaProps.MaxItems = props.MaxItems;
            betaProps.MinItems = props.MinItems;
            betaProps.UniqueItems = props.UniqueItems;

            betaProps.MaxLength = props.MaxLength;
            betaProps.MinLength = props.MinLength;

            betaProps.MultipleOf = props.MultipleOf;

            betaProps.Pattern = props.Pattern;

            betaProps.Maximum = props.Maximum;
            betaProps.ExclusiveMaximum = props.ExclusiveMaximum;

            betaProps.Minimum = props.Minimum;
            betaProps.ExclusiveMinimum = props.ExclusiveMinimum;

            if (props.Properties != null)
            {
                betaProps.Properties = new Dictionary<string, V1beta1JSONSchemaProps>(
                    props.Properties.Select(p => KeyValuePair.Create(p.Key, p.Value.Convert())));
            }

            betaProps.Type = props.Type;
            betaProps.Format = props.Format;
            betaProps.Items = (props.Items as V1JSONSchemaProps)?.Convert();
            betaProps.Required = props.Required;
            betaProps.EnumProperty = props.EnumProperty;

            return betaProps;
        }
    }
}
