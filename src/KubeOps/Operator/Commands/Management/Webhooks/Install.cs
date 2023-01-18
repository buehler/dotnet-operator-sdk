using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.KubernetesClient.LabelSelectors;
using KubeOps.Operator.Commands.CommandHelpers;
using KubeOps.Operator.Entities.Extensions;
using KubeOps.Operator.Webhooks;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Management.Webhooks;

[Command(
    "install",
    "i",
    Description = "This installs the needed elements for webhooks and the authentication on the current cluster. " +
                  "First, certificates will be generated, then a service is created and the validation config is created.")]
internal class Install
{
    private readonly OperatorSettings _settings;
    private readonly ValidatingWebhookConfigurationBuilder _validatingWebhookConfigurationBuilder;
    private readonly MutatingWebhookConfigurationBuilder _mutatingWebhookConfigurationBuilder;

    public Install(
        OperatorSettings settings,
        ValidatingWebhookConfigurationBuilder validatingWebhookConfigurationBuilder,
        MutatingWebhookConfigurationBuilder mutatingWebhookConfigurationBuilder)
    {
        _settings = settings;
        _validatingWebhookConfigurationBuilder = validatingWebhookConfigurationBuilder;
        _mutatingWebhookConfigurationBuilder = mutatingWebhookConfigurationBuilder;
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
        var client = app.GetRequiredService<IKubernetesClient>();

        var @namespace = await client.GetCurrentNamespace();
        using var certManager = new CertificateGenerator(app.Out);

#if DEBUG
        CertificatesPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        CaCertificatesPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await certManager.CreateCaCertificateAsync(CaCertificatesPath);
#endif

        Directory.CreateDirectory(CertificatesPath);
        if (File.Exists(Path.Join(Path.Join(CertificatesPath, "ca.pem"))))
        {
            File.Delete(Path.Join(Path.Join(CertificatesPath, "ca.pem")));
        }

        File.Copy(Path.Join(CaCertificatesPath, "ca.pem"), Path.Join(Path.Join(CertificatesPath, "ca.pem")));
        await certManager.CreateServerCertificateAsync(
            CertificatesPath,
            _settings.Name,
            @namespace,
            Path.Join(CaCertificatesPath, "ca.pem"),
            Path.Join(CaCertificatesPath, "ca-key.pem"));

        var deployment = (await client.List<V1Deployment>(
            @namespace,
            new EqualsSelector("operator-deployment", _settings.Name))).FirstOrDefault();
        if (deployment != null)
        {
            deployment.Kind = V1Deployment.KubeKind;
            deployment.ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}";
        }

        await app.Out.WriteLineAsync("Create service.");
        await client.Delete<V1Service>(_settings.Name, @namespace);
        await client.Create(
            new V1Service(
                V1Service.KubeApiVersion,
                V1Service.KubeKind,
                new(
                    name: _settings.Name,
                    namespaceProperty: @namespace,
                    ownerReferences: deployment != null
                        ? new List<V1OwnerReference> { deployment.MakeOwnerReference(), }
                        : null,
                    labels: new Dictionary<string, string>
                    {
                        { "operator", _settings.Name }, { "usage", "webhook-service" },
                    }),
                new()
                {
                    Ports =
                        new List<V1ServicePort> { new() { Name = "https", TargetPort = "https", Port = 443, }, },
                    Selector = new Dictionary<string, string> { { "operator", _settings.Name }, },
                }));

        var caBundle = await File.ReadAllBytesAsync(Path.Join(CertificatesPath, "ca.pem"));
        var webhookConfig = new WebhookConfig(
            _settings.Name,
            null,
            caBundle,
            new() { Name = _settings.Name, NamespaceProperty = @namespace, });

        await app.Out.WriteLineAsync("Create validator definition.");
        var validatorConfig = _validatingWebhookConfigurationBuilder.BuildWebhookConfiguration(webhookConfig);
        if (deployment != null)
        {
            validatorConfig.Metadata.OwnerReferences = new List<V1OwnerReference> { deployment.MakeOwnerReference(), };
        }

        await client.Save(validatorConfig);

        await app.Out.WriteLineAsync("Create mutator definition.");
        var mutatorConfig = _mutatingWebhookConfigurationBuilder.BuildWebhookConfiguration(webhookConfig);
        if (deployment != null)
        {
            mutatorConfig.Metadata.OwnerReferences = new List<V1OwnerReference> { deployment.MakeOwnerReference(), };
        }

        await client.Save(mutatorConfig);

        await app.Out.WriteLineAsync("Installed webhook service and admission configurations.");

        return ExitCodes.Success;
    }
}
