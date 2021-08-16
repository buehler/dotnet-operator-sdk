using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotnetKubernetesClient.Entities;
using k8s.Models;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Util;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Webhooks
{
    internal class MutatingWebhookBuilder
    {
        private readonly IComponentRegistrar _componentRegistrar;
        private readonly IWebhookMetadataBuilder _webhookMetadataBuilder;
        private readonly IServiceProvider _services;

        public MutatingWebhookBuilder(
            IComponentRegistrar componentRegistrar,
            IWebhookMetadataBuilder webhookMetadataBuilder,
            IServiceProvider services)
        {
            _componentRegistrar = componentRegistrar;
            _webhookMetadataBuilder = webhookMetadataBuilder;
            _services = services;
        }

        public List<V1MutatingWebhook> BuildWebhooks(WebhookConfig webhookConfig)
            => _componentRegistrar.MutatorRegistrations
                .Select(
                    wh =>
                    {
                        (Type mutatorType, Type entityType) = wh;

                        var instance = _services.GetRequiredService(mutatorType);

                        var (name, endpoint) = _webhookMetadataBuilder.GetMetadata<MutationResult>(instance, entityType);

                        var clientConfig = new Admissionregistrationv1WebhookClientConfig();
                        if (!string.IsNullOrWhiteSpace(webhookConfig.BaseUrl))
                        {
                            clientConfig.Url = webhookConfig.BaseUrl.FormatWebhookUrl(endpoint);
                        }
                        else
                        {
                            clientConfig.Service = webhookConfig.Service;
                            if (clientConfig.Service != null)
                            {
                                clientConfig.Service.Path = endpoint;
                            }

                            clientConfig.CaBundle = webhookConfig.CaBundle;
                        }

                        var operationsProperty = typeof(IAdmissionWebhook<,>)
                            .MakeGenericType(entityType, typeof(MutationResult))
                            .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                            .First(m => m.Name == "SupportedOperations");

                        var crd = entityType.CreateResourceDefinition();

                        return new V1MutatingWebhook
                        {
                            Name = name.TrimWebhookName(),
                            AdmissionReviewVersions = new[] { "v1" },
                            SideEffects = "None",
                            MatchPolicy = "Exact",
                            Rules = new List<V1RuleWithOperations>
                            {
                                new()
                                {
                                    Operations = operationsProperty.GetValue(instance) as IList<string>,
                                    Resources = new[] { crd.Plural },
                                    Scope = "*",
                                    ApiGroups = new[] { crd.Group },
                                    ApiVersions = new[] { crd.Version },
                                },
                            },
                            ClientConfig = clientConfig,
                        };
                    })
                .ToList();
    }
}
