using System.Text;
using KubeOps.KubernetesClient.Entities;

namespace KubeOps.Operator.Webhooks
{
    internal static class WebhookEndpointFactory
    {
        internal static string Create<TEntity>(Type owningType, string suffix)
        {
            var crd = typeof(TEntity).ToEntityDefinition();

            var builder = new StringBuilder();

            if (!string.IsNullOrEmpty(crd.Group))
            {
                builder.Append('/').Append(crd.Group);
            }

            if (!string.IsNullOrEmpty(crd.Version))
            {
                builder.Append('/').Append(crd.Version);
            }

            if (!string.IsNullOrEmpty(crd.Plural))
            {
                builder.Append('/').Append(crd.Plural);
            }

            builder.Append('/').Append(owningType.Name);
            builder.Append(suffix);

            return builder.ToString().ToLowerInvariant();
        }
    }
}
