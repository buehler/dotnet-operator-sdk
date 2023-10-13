using System.Reflection;

using KubeOps.Operator.Web.Webhooks.Mutation;
using KubeOps.Operator.Web.Webhooks.Validation;

namespace KubeOps.Operator.Web.LocalTunnel;

internal record WebhookLoader(Assembly Entry)
{
    public IEnumerable<TypeInfo> ValidationWebhooks => Entry
        .DefinedTypes
        .Where(t => t.BaseType?.IsGenericType == true &&
                    t.BaseType?.GetGenericTypeDefinition() == typeof(ValidationWebhook<>));

    public IEnumerable<TypeInfo> MutationWebhooks => Entry
        .DefinedTypes
        .Where(t => t.BaseType?.IsGenericType == true &&
                    t.BaseType?.GetGenericTypeDefinition() == typeof(MutationWebhook<>));
}
