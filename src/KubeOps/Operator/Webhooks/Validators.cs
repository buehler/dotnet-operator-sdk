using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotnetKubernetesClient.Entities;
using k8s.Models;
using KubeOps.Operator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Webhooks
{
    internal static class Validators
    {
        private const byte MaxNameLength = 254;

        public static V1ValidatingWebhookConfiguration CreateValidator(
            (string OperatorName, string? BaseUrl, byte[]? CaBundle, Admissionregistrationv1ServiceReference? Service)
                hookConfig,
            ResourceLocator locator,
            IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            return new()
            {
                Kind = V1ValidatingWebhookConfiguration.KubeKind,
                ApiVersion =
                    $"{V1ValidatingWebhookConfiguration.KubeGroup}/{V1ValidatingWebhookConfiguration.KubeApiVersion}",
                Metadata = new V1ObjectMeta
                {
                    Name = TrimName($"validators.{hookConfig.OperatorName}").ToLowerInvariant(),
                },
                Webhooks = locator
                    .ValidatorTypes
                    .Select(
                        wh =>
                        {
                            var (validatorType, resourceType) = wh;

                            var crd = resourceType.CreateResourceDefinition();
                            var endpoint = $"/{crd.Group}/{crd.Version}/{crd.Plural}/validate".ToLowerInvariant();

                            var clientConfig = new Admissionregistrationv1WebhookClientConfig();
                            if (!string.IsNullOrWhiteSpace(hookConfig.BaseUrl))
                            {
                                clientConfig.Url = WebhookUrl(hookConfig.BaseUrl, endpoint);
                            }
                            else
                            {
                                clientConfig.Service = hookConfig.Service;
                                if (clientConfig.Service != null)
                                {
                                    clientConfig.Service.Path = endpoint;
                                }

                                clientConfig.CaBundle = hookConfig.CaBundle;
                            }

                            var instance = scope.ServiceProvider.GetRequiredService(validatorType);
                            var operationsProperty = typeof(IValidationWebhook<>)
                                .MakeGenericType(resourceType)
                                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                                .First(m => m.Name == "SupportedOperations");
                            var webhookNameProperty = typeof(IValidationWebhook<>)
                                .MakeGenericType(resourceType)
                                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                                .First(m => m.Name == "WebhookName");

                            return new V1ValidatingWebhook
                            {
                                Name = TrimName(
                                        webhookNameProperty.GetValue(instance) as string ??
                                        throw new Exception("Webhook name is null."))
                                    .ToLowerInvariant(),
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
                    .ToList(),
            };
        }

        private static string TrimName(string name) =>
            name.Length < MaxNameLength ? name : name.Substring(0, MaxNameLength);

        private static string WebhookUrl(string baseUrl, string endpoint)
        {
            if (!baseUrl.StartsWith("https://"))
            {
                throw new ArgumentException(@"The base url must start with ""https://"".");
            }

            return baseUrl.Trim().TrimEnd('/') + endpoint;
        }
    }
}
