using System.Reflection;

using KubeOps.Abstractions.Entities;

namespace KubeOps.Cli.Transpilation;

internal record ValidationWebhook(TypeInfo Validator, EntityMetadata Metadata) : BaseWebhook(Validator, Metadata)
{
    public override string WebhookPath =>
        $"/validate/{Validator.BaseType!.GenericTypeArguments[0].Name.ToLowerInvariant()}";
}
