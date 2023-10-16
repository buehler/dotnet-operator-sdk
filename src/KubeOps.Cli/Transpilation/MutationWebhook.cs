using System.Reflection;

using KubeOps.Abstractions.Entities;

namespace KubeOps.Cli.Transpilation;

internal record MutationWebhook(TypeInfo Validator, EntityMetadata Metadata) : BaseWebhook(Validator, Metadata)
{
    public override string WebhookPath =>
        $"/mutate/{Validator.BaseType!.GenericTypeArguments[0].Name.ToLowerInvariant()}";
}
