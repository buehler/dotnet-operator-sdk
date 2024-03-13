using System.Reflection;
using System.Runtime.Versioning;

using KubeOps.Operator.Web.Webhooks.Admission.Mutation;
using KubeOps.Operator.Web.Webhooks.Admission.Validation;
using KubeOps.Operator.Web.Webhooks.Conversion;

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

    public IEnumerable<TypeInfo> ConversionWebhooks => Entry
        .DefinedTypes
        .Where(t => t.BaseType?.IsGenericType == true &&
#pragma warning disable CA2252 // This is internal only.
                    t.BaseType?.GetGenericTypeDefinition() == typeof(ConversionWebhook<>));
#pragma warning restore CA2252
}
