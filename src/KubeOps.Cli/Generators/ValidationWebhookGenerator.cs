using k8s;
using k8s.Models;

using KubeOps.Cli.Output;
using KubeOps.Cli.Transpilation;

namespace KubeOps.Cli.Generators;

internal class ValidationWebhookGenerator
    (List<ValidationWebhook> webhooks, byte[] caBundle, OutputFormat format) : IConfigGenerator
{
    public void Generate(ResultOutput output)
    {
        if (webhooks.Count == 0)
        {
            return;
        }

        var validatorConfig = new V1ValidatingWebhookConfiguration(
            metadata: new V1ObjectMeta(name: "validators"),
            webhooks: new List<V1ValidatingWebhook>()).Initialize();

        foreach (var hook in webhooks)
        {
            var names = new List<string> { hook.Metadata.SingularName, hook.Metadata.Group ?? string.Empty, hook.Metadata.Version };
            var hookName = string.Join(".", names.Where(name => !string.IsNullOrWhiteSpace(name)));
            validatorConfig.Webhooks.Add(new V1ValidatingWebhook
            {
                Name = $"validate.{hookName}",
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
            $"validators.{format.GetFileExtension()}", validatorConfig);
    }
}
