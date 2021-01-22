using System;
using System.Collections.Generic;
using System.Linq;
using DotnetKubernetesClient.Entities;
using k8s.Models;

namespace KubeOps.Operator.Webhooks
{
    internal static class Validators
    {
        private const byte MaxNameLength = 254;

        public static V1ValidatingWebhookConfiguration CreateValidator(
            (string OperatorName, string? BaseUrl, byte[]? CaBundle, Admissionregistrationv1ServiceReference? Service)
                hookConfig,
            IEnumerable<IValidationWebhook> validators) =>
            new()
            {
                Kind = V1ValidatingWebhookConfiguration.KubeKind,
                ApiVersion =
                    $"{V1ValidatingWebhookConfiguration.KubeGroup}/{V1ValidatingWebhookConfiguration.KubeApiVersion}",
                Metadata = new V1ObjectMeta
                {
                    Name = TrimName($"validators.{hookConfig.OperatorName}").ToLowerInvariant(),
                },
                Webhooks = validators
                    .Select(
                        wh =>
                        {
                            var type = wh
                                .GetType()
                                .GetInterfaces()
                                .FirstOrDefault(
                                    t => t.IsGenericType &&
                                         typeof(IValidationWebhook<>).IsAssignableFrom(t.GetGenericTypeDefinition()));
                            if (type == null)
                            {
                                throw new Exception(
                                    $@"Validator ""{wh.GetType().Name}"" is not of IValidationWebhook<TEntity> type.");
                            }

                            var crd = type
                                .GenericTypeArguments
                                .First()
                                .CreateResourceDefinition();

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

                            return new V1ValidatingWebhook
                            {
                                Name = TrimName(wh.WebhookName).ToLowerInvariant(),
                                AdmissionReviewVersions = new[] { "v1" },
                                SideEffects = "None",
                                MatchPolicy = "Exact",
                                Rules = new List<V1RuleWithOperations>
                                {
                                    new()
                                    {
                                        Operations = wh.SupportedOperations,
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
