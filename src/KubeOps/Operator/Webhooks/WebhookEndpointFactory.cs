using System.Text;
using DotnetKubernetesClient.Entities;

namespace KubeOps.Operator.Webhooks
{
    internal static class WebhookEndpointFactory
    {
        internal static string Create<TEntity>(Type owningType, string suffix)
        {
            var crd = typeof(TEntity).CreateResourceDefinition();

            var builder = new StringBuilder();

            if (!string.IsNullOrEmpty(crd.Group))
            {
                builder.Append($"/{crd.Group}");
            }

            if (!string.IsNullOrEmpty(crd.Version))
            {
                builder.Append($"/{crd.Version}");
            }

            if (!string.IsNullOrEmpty(crd.Plural))
            {
                builder.Append($"/{crd.Plural}");
            }

            builder.Append($"/{owningType.Name}");
            builder.Append(suffix);

            return builder.ToString().ToLowerInvariant();
        }
    }
}
