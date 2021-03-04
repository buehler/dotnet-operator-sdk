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
    internal static class Webhooks
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
                            var instance = scope.ServiceProvider.GetRequiredService(validatorType);

                            var (name, endpoint) = Metadata<ValidationResult>(instance, resourceType);

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

                            var operationsProperty = typeof(IAdmissionWebhook<,>)
                                .MakeGenericType(resourceType, typeof(ValidationResult))
                                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                                .First(m => m.Name == "SupportedOperations");

                            var crd = resourceType.CreateResourceDefinition();

                            return new V1ValidatingWebhook
                            {
                                Name = TrimName(name),
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

        public static V1MutatingWebhookConfiguration CreateMutator(
            (string OperatorName, string? BaseUrl, byte[]? CaBundle, Admissionregistrationv1ServiceReference? Service)
                hookConfig,
            ResourceLocator locator,
            IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            return new()
            {
                Kind = V1MutatingWebhookConfiguration.KubeKind,
                ApiVersion =
                    $"{V1MutatingWebhookConfiguration.KubeGroup}/{V1MutatingWebhookConfiguration.KubeApiVersion}",
                Metadata = new V1ObjectMeta
                {
                    Name = TrimName($"mutators.{hookConfig.OperatorName}").ToLowerInvariant(),
                },
                Webhooks = locator
                    .MutatorTypes
                    .Select(
                        wh =>
                        {
                            var (mutatorType, resourceType) = wh;
                            var instance = scope.ServiceProvider.GetRequiredService(mutatorType);

                            var (name, endpoint) = Metadata<MutationResult>(instance, resourceType);

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

                            var operationsProperty = typeof(IAdmissionWebhook<,>)
                                .MakeGenericType(resourceType, typeof(MutationResult))
                                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                                .First(m => m.Name == "SupportedOperations");

                            var crd = resourceType.CreateResourceDefinition();

                            return new V1MutatingWebhook
                            {
                                Name = TrimName(name).ToLowerInvariant(),
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

        public static (string Name, string Endpoint) Metadata<TResult>(object hook, Type resourceType)
        {
            var nameProperty = typeof(IAdmissionWebhook<,>)
                .MakeGenericType(resourceType, typeof(TResult))
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                .First(p => p.Name == "Name");
            var endpointProperty = typeof(IAdmissionWebhook<,>)
                .MakeGenericType(resourceType, typeof(TResult))
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                .First(p => p.Name == "Endpoint");

            return (
                nameProperty.GetValue(hook)?.ToString() ?? string.Empty,
                endpointProperty.GetValue(hook)?.ToString() ?? string.Empty);
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
