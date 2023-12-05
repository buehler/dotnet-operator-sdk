using System.Reflection;

using k8s.Models;

using KubeOps.Cli.Output;
using KubeOps.Cli.Transpilation;
using KubeOps.Transpiler;

namespace KubeOps.Cli.Generators;

internal class CrdGenerator(MetadataLoadContext parser, byte[] caBundle,
    OutputFormat outputFormat) : IConfigGenerator
{
    public void Generate(ResultOutput output)
    {
        var crds = parser.Transpile(parser.GetEntities()).ToList();
        var conversionWebhooks = parser.GetConvertedEntities().ToList();

        foreach (var crd in crds)
        {
            if (conversionWebhooks
                    .Find(wh => crd.Spec.Group == wh.Group && crd.Spec.Names.Kind == wh.Kind) is not null)
            {
                crd.Spec.Conversion = new V1CustomResourceConversion
                {
                    Strategy = "Webhook",
                    Webhook = new V1WebhookConversion
                    {
                        ConversionReviewVersions = new[] { "v1" },
                        ClientConfig = new Apiextensionsv1WebhookClientConfig
                        {
                            CaBundle = caBundle,
                            Service = new Apiextensionsv1ServiceReference
                            {
                                Path = $"/convert/{crd.Spec.Group}/{crd.Spec.Names.Plural}",
                                Name = "service",
                            },
                        },
                    },
                };
            }

            output.Add($"{crd.Metadata.Name.Replace('.', '_')}.{outputFormat.GetFileExtension()}", crd);
        }
    }
}
