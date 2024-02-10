using k8s;
using k8s.Models;

using KubeOps.Cli.Output;
using KubeOps.Cli.Transpilation;

namespace KubeOps.Cli.Generators;

internal class MutationWebhookGenerator
    (List<MutationWebhook> webhooks, byte[] caBundle, OutputFormat format) : IConfigGenerator
{
    public void Generate(ResultOutput output)
    {
        if (webhooks.Count == 0)
        {
            return;
        }

        var mutatorConfig = new V1MutatingWebhookConfiguration(
            metadata: new V1ObjectMeta(name: "mutators"),
            webhooks: new List<V1MutatingWebhook>()).Initialize();

        foreach (var hook in webhooks)
        {
            var hookName = string.Join(
                ".",
                new List<string>
                {
                    hook.Metadata.SingularName, hook.Metadata.Group, hook.Metadata.Version,
                }.Where(name => !string.IsNullOrWhiteSpace(name)));
            mutatorConfig.Webhooks.Add(new V1MutatingWebhook
            {
                Name = $"mutate.{hookName}",
                MatchPolicy = "Exact",
                AdmissionReviewVersions = new[] { "v1" },
                SideEffects = "None",
                Rules = new[]
                {
                    new V1RuleWithOperations
                    {
                        Operations = hook.GetOperations(),
                        Resources = new[] { hook.Metadata.PluralName },
                        ApiGroups = new[] { hook.Metadata.Group },
                        ApiVersions = new[] { hook.Metadata.Version },
                    },
                },
                ClientConfig = new Admissionregistrationv1WebhookClientConfig
                {
                    CaBundle = caBundle,
                    Service = new Admissionregistrationv1ServiceReference
                    {
                        Name = "operator",
                        Path = hook.WebhookPath,
                    },
                },
            });
        }

        output.Add(
            $"mutators.{format.GetFileExtension()}", mutatorConfig);
    }
}
