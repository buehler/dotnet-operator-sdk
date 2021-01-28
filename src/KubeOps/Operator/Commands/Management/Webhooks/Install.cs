﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using DotnetKubernetesClient.LabelSelectors;
using k8s.Models;
using KubeOps.Operator.Commands.CommandHelpers;
using KubeOps.Operator.Entities.Extensions;
using KubeOps.Operator.Services;
using KubeOps.Operator.Webhooks;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Management.Webhooks
{
    [Command(
        "install",
        "i",
        Description = "This installs the needed elements for webhooks and the authentication on the current cluster. " +
                      "First, certificates will be generated, then a service is created and the validation config is created.")]
    internal class Install
    {
        private readonly IKubernetesClient _client;
        private readonly OperatorSettings _settings;
        private readonly ResourceLocator _resourceLocator;
        private readonly IServiceProvider _serviceProvider;

        public Install(
            IKubernetesClient client,
            OperatorSettings settings,
            ResourceLocator resourceLocator,
            IServiceProvider serviceProvider)
        {
            _client = client;
            _settings = settings;
            _resourceLocator = resourceLocator;
            _serviceProvider = serviceProvider;
        }

        [Option(
            Description =
                @"The ""root"" for the generated certificates. Those certificates are needed for webhook TLS.",
            ShortName = "x",
            LongName = "certs")]
        public string CertificatesPath { get; set; } = "/certs";

        [Option(
            Description =
                "The folder where the ca.pem and ca-key.pem files are located. Needed for generation of the server certificate.",
            ShortName = "z",
            LongName = "ca-certs")]
        public string CaCertificatesPath { get; set; } = "/ca";

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var @namespace = await _client.GetCurrentNamespace();
            using var certManager = new CertificateGenerator(app.Out);

#if DEBUG
            CertificatesPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            CaCertificatesPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            await certManager.CreateCaCertificateAsync(CaCertificatesPath);
#endif

            Directory.CreateDirectory(CertificatesPath);
            File.Copy(Path.Join(CaCertificatesPath, "ca.pem"), Path.Join(Path.Join(CertificatesPath, "ca.pem")));
            await certManager.CreateServerCertificateAsync(
                CertificatesPath,
                _settings.Name,
                @namespace,
                Path.Join(CaCertificatesPath, "ca.pem"),
                Path.Join(CaCertificatesPath, "ca-key.pem"));

            var deployment = (await _client.List<V1Deployment>(
                @namespace,
                new EqualsSelector("operator-deployment", _settings.Name))).FirstOrDefault();
            if (deployment != null)
            {
                deployment.Kind = V1Deployment.KubeKind;
                deployment.ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}";
            }

            await app.Out.WriteLineAsync("Create service.");
            try
            {
                await _client.Delete<V1Service>(_settings.Name, @namespace);
            }
            catch
            {
                // Ignore not found service delete.
            }

            await _client.Create(
                new V1Service(
                    V1Service.KubeApiVersion,
                    V1Service.KubeKind,
                    new V1ObjectMeta(
                        name: _settings.Name,
                        namespaceProperty: @namespace,
                        ownerReferences: deployment != null
                            ? new List<V1OwnerReference>
                            {
                                deployment.MakeOwnerReference(),
                            }
                            : null,
                        labels: new Dictionary<string, string>
                        {
                            { "operator", _settings.Name },
                            { "usage", "webhook-service" },
                        }),
                    new V1ServiceSpec
                    {
                        Ports = new List<V1ServicePort>
                        {
                            new()
                            {
                                Name = "https",
                                TargetPort = "https",
                                Port = 443,
                            },
                        },
                        Selector = new Dictionary<string, string>
                        {
                            { "operator", _settings.Name },
                        },
                    }));

            await app.Out.WriteLineAsync("Create validator definition.");
            try
            {
                await _client.Delete<V1ValidatingWebhookConfiguration>($"validators.{_settings.Name}", @namespace);
            }
            catch
            {
                // Ignore not found service delete.
            }

            var caBundle = await File.ReadAllBytesAsync(Path.Join(CertificatesPath, "ca.pem"));
            var validator = Validators.CreateValidator(
                (_settings.Name, null, caBundle, new Admissionregistrationv1ServiceReference
                {
                    Name = _settings.Name,
                    NamespaceProperty = @namespace,
                }),
                _resourceLocator,
                _serviceProvider);
            if (deployment != null)
            {
                validator.Metadata.OwnerReferences = new List<V1OwnerReference>
                {
                    deployment.MakeOwnerReference(),
                };
            }

            await _client.Create(validator);
            return ExitCodes.Success;
        }
    }
}
