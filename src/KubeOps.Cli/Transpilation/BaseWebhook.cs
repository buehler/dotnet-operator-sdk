using System.Reflection;

using KubeOps.Abstractions.Entities;

namespace KubeOps.Cli.Transpilation;

internal abstract record BaseWebhook(TypeInfo Webhook, EntityMetadata Metadata)
{
    public abstract string WebhookPath { get; }

    private bool HasCreate => Webhook.DeclaredMembers.Any(m => m.Name.StartsWith("Create"));

    private bool HasUpdate => Webhook.DeclaredMembers.Any(m => m.Name.StartsWith("Update"));

    private bool HasDelete => Webhook.DeclaredMembers.Any(m => m.Name.StartsWith("Delete"));

    public string[] GetOperations() =>
        new[] { HasCreate ? "CREATE" : null, HasUpdate ? "UPDATE" : null, HasDelete ? "DELETE" : null, }
            .Where(o => o is not null).ToArray()!;
}
