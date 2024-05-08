﻿using k8s;
using k8s.Models;

using KubeOps.KubernetesClient;
using KubeOps.Transpiler;

namespace KubeOps.Operator.Web.Webhooks
{
    internal abstract class WebhookServiceBase(IKubernetesClient client, WebhookLoader loader, WebhookConfig config)
    {
        /// <summary>
        /// The URI the webhooks will use to connect to the operator.
        /// </summary>
        private protected virtual Uri Uri { get; set; } = new($"https://{config.Hostname}:{config.Port}");

        private protected IKubernetesClient Client { get; } = client;

        /// <summary>
        /// The PEM-encoded CA bundle for validating the webhook's certificate.
        /// </summary>
        private protected byte[]? CaBundle { get; set; }

        internal async Task RegisterAll()
        {
            await RegisterValidators();
            await RegisterMutators();
            await RegisterConverters();
        }

        internal async Task RegisterConverters()
        {
            var conversionWebhooks = loader.ConversionWebhooks.ToList();
            if (conversionWebhooks.Count == 0)
            {
                return;
            }

            foreach (var wh in conversionWebhooks)
            {
                var metadata = Entities.ToEntityMetadata(wh.BaseType!.GenericTypeArguments[0]).Metadata;
                var crdName = $"{metadata.PluralName}.{metadata.Group}";

                if (await Client.GetAsync<V1CustomResourceDefinition>(crdName) is not { } crd)
                {
                    continue;
                }

                var whUrl = $"{Uri}convert/{metadata.Group}/{metadata.PluralName}";
                crd.Spec.Conversion = new V1CustomResourceConversion("Webhook")
                {
                    Webhook = new V1WebhookConversion
                    {
                        ConversionReviewVersions = new[] { "v1" },
                        ClientConfig = new Apiextensionsv1WebhookClientConfig
                        {
                            Url = whUrl,
                            CaBundle = CaBundle,
                        },
                    },
                };

                await Client.UpdateAsync(crd);
            }
        }

        internal async Task RegisterMutators()
        {
            var mutationWebhooks = loader
                .MutationWebhooks
                .Select(t => (HookTypeName: t.BaseType!.GenericTypeArguments[0].Name.ToLowerInvariant(),
                    Entities.ToEntityMetadata(t.BaseType!.GenericTypeArguments[0]).Metadata))
                .Select(hook => new V1MutatingWebhook
                {
                    Name = $"mutate.{hook.Metadata.SingularName}.{hook.Metadata.Group}.{hook.Metadata.Version}",
                    MatchPolicy = "Exact",
                    AdmissionReviewVersions = new[] { "v1" },
                    SideEffects = "None",
                    Rules = new[]
                    {
                    new V1RuleWithOperations
                    {
                        Operations = new[] { "*" },
                        Resources = new[] { hook.Metadata.PluralName },
                        ApiGroups = new[] { hook.Metadata.Group },
                        ApiVersions = new[] { hook.Metadata.Version },
                    },
                    },
                    ClientConfig = new Admissionregistrationv1WebhookClientConfig
                    {
                        Url = $"{Uri}mutate/{hook.HookTypeName}",
                        CaBundle = CaBundle,
                    },
                });

            var mutatorConfig = new V1MutatingWebhookConfiguration(
                metadata: new V1ObjectMeta(name: "dev-mutators"),
                webhooks: mutationWebhooks.ToList()).Initialize();

            if (mutatorConfig.Webhooks.Any())
            {
                await Client.SaveAsync(mutatorConfig);
            }
        }

        internal async Task RegisterValidators()
        {
            var validationWebhooks = loader
                .ValidationWebhooks
                .Select(t => (HookTypeName: t.BaseType!.GenericTypeArguments[0].Name.ToLowerInvariant(),
                    Entities.ToEntityMetadata(t.BaseType!.GenericTypeArguments[0]).Metadata))
                .Select(hook => new V1ValidatingWebhook
                {
                    Name = $"validate.{hook.Metadata.SingularName}.{hook.Metadata.Group}.{hook.Metadata.Version}",
                    MatchPolicy = "Exact",
                    AdmissionReviewVersions = new[] { "v1" },
                    SideEffects = "None",
                    Rules = new[]
                    {
                    new V1RuleWithOperations
                    {
                        Operations = new[] { "*" },
                        Resources = new[] { hook.Metadata.PluralName },
                        ApiGroups = new[] { hook.Metadata.Group },
                        ApiVersions = new[] { hook.Metadata.Version },
                    },
                    },
                    ClientConfig = new Admissionregistrationv1WebhookClientConfig
                    {
                        Url = $"{Uri}validate/{hook.HookTypeName}",
                        CaBundle = CaBundle,
                    },
                });

            var validatorConfig = new V1ValidatingWebhookConfiguration(
                metadata: new V1ObjectMeta(name: "dev-validators"),
                webhooks: validationWebhooks.ToList()).Initialize();

            if (validatorConfig.Webhooks.Any())
            {
                await Client.SaveAsync(validatorConfig);
            }
        }
    }
}
