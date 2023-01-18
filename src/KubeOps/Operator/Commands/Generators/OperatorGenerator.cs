using k8s.Models;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Commands.CommandHelpers;
using KubeOps.Operator.Entities.Kustomize;
using KubeOps.Operator.Serialization;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators;

[Command("operator", "op", Description = "Generates the needed yamls to run the operator.")]
internal class OperatorGenerator : GeneratorBase
{
    private readonly OperatorSettings _settings;
    private readonly bool _hasWebhooks;

    public OperatorGenerator(
        OperatorSettings settings,
        IComponentRegistrar componentRegistrar)
    {
        _settings = settings;

        _hasWebhooks = componentRegistrar.ValidatorRegistrations.Any() ||
                       componentRegistrar.MutatorRegistrations.Any();
    }

    public async Task<int> OnExecuteAsync(CommandLineApplication app)
    {
        var fileWriter = new FileWriter(app.Out);

        if (OutputPath != null &&
            _hasWebhooks &&
            (!File.Exists(Path.Join(OutputPath, "ca.pem")) || !File.Exists(Path.Join(OutputPath, "ca-key.pem"))))
        {
            using var certManager = new CertificateGenerator(app.Out);
            await certManager.CreateCaCertificateAsync(OutputPath);
        }

        fileWriter.Add(
            $"kustomization.{Format.ToString().ToLower()}",
            EntitySerializer.Serialize(
                new KustomizationConfig
                {
                    Resources = new List<string> { $"deployment.{Format.ToString().ToLower()}", },
                    CommonLabels = new Dictionary<string, string> { { "operator-element", "operator-instance" }, },
                    ConfigMapGenerator = _hasWebhooks
                        ? new List<KustomizationConfigMapGenerator>
                        {
                            new() { Name = "webhook-ca", Files = new List<string> { "ca.pem", "ca-key.pem", }, },
                            new()
                            {
                                Name = "webhook-config",
                                Literals = new List<string>
                                {
                                    $"KESTREL__ENDPOINTS__HTTP__URL=http://0.0.0.0:{_settings.HttpPort}",
                                    $"KESTREL__ENDPOINTS__HTTPS__URL=https://0.0.0.0:{_settings.HttpsPort}",
                                    "KESTREL__ENDPOINTS__HTTPS__CERTIFICATE__PATH=/certs/server.pem",
                                    "KESTREL__ENDPOINTS__HTTPS__CERTIFICATE__KEYPATH=/certs/server-key.pem",
                                },
                            },
                        }
                        : new List<KustomizationConfigMapGenerator>
                        {
                            new()
                            {
                                Name = "webhook-config",
                                Literals = new List<string>
                                {
                                    $"KESTREL__ENDPOINTS__HTTP__URL=http://0.0.0.0:{_settings.HttpPort}",
                                },
                            },
                        },
                },
                Format));

        fileWriter.Add(
            $"deployment.{Format.ToString().ToLower()}",
            EntitySerializer.Serialize(
                new V1Deployment(
                    $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                    V1Deployment.KubeKind,
                    new V1ObjectMeta(
                        name: "operator",
                        labels: new Dictionary<string, string> { { "operator-deployment", _settings.Name } }),
                    new V1DeploymentSpec
                    {
                        Replicas = 1,
                        RevisionHistoryLimit = 0,
                        Selector = new V1LabelSelector(
                            matchLabels: new Dictionary<string, string> { { "operator", _settings.Name } }),
                        Template = new V1PodTemplateSpec
                        {
                            Metadata = new V1ObjectMeta(
                                labels: new Dictionary<string, string> { { "operator", _settings.Name } }),
                            Spec = new V1PodSpec
                            {
                                TerminationGracePeriodSeconds = 10,
                                Volumes = !_hasWebhooks
                                    ? null
                                    : new List<V1Volume>
                                    {
                                        new() { Name = "certificates", EmptyDir = new(), },
                                        new()
                                        {
                                            Name = "ca-certificates", ConfigMap = new() { Name = "webhook-ca" },
                                        },
                                    },
                                InitContainers = !_hasWebhooks
                                    ? null
                                    : new List<V1Container>
                                    {
                                        new()
                                        {
                                            Image = "operator",
                                            Name = "webhook-installer",
                                            Args = new[] { "webhooks", "install", },
                                            Env = new List<V1EnvVar>
                                            {
                                                new()
                                                {
                                                    Name = "POD_NAMESPACE",
                                                    ValueFrom = new V1EnvVarSource
                                                    {
                                                        FieldRef = new V1ObjectFieldSelector
                                                        {
                                                            FieldPath = "metadata.namespace",
                                                        },
                                                    },
                                                },
                                            },
                                            VolumeMounts = new List<V1VolumeMount>
                                            {
                                                new() { Name = "certificates", MountPath = "/certs", },
                                                new()
                                                {
                                                    Name = "ca-certificates",
                                                    MountPath = "/ca",
                                                    ReadOnlyProperty = true,
                                                },
                                            },
                                        },
                                    },
                                Containers = new List<V1Container>
                                {
                                    new()
                                    {
                                        Image = "operator",
                                        Name = "operator",
                                        Env = new List<V1EnvVar>
                                        {
                                            new()
                                            {
                                                Name = "POD_NAMESPACE",
                                                ValueFrom = new V1EnvVarSource
                                                {
                                                    FieldRef = new V1ObjectFieldSelector
                                                    {
                                                        FieldPath = "metadata.namespace",
                                                    },
                                                },
                                            },
                                        },
                                        EnvFrom = new List<V1EnvFromSource>
                                        {
                                            new() { ConfigMapRef = new() { Name = "webhook-config" } },
                                        },
                                        VolumeMounts = !_hasWebhooks
                                            ? null
                                            : new List<V1VolumeMount>
                                            {
                                                new()
                                                {
                                                    Name = "certificates",
                                                    MountPath = "/certs",
                                                    ReadOnlyProperty = true,
                                                },
                                            },
                                        Ports = _hasWebhooks
                                            ? new List<V1ContainerPort>
                                            {
                                                new(_settings.HttpPort, name: "http"),
                                                new(_settings.HttpsPort, name: "https"),
                                            }
                                            : new List<V1ContainerPort> { new(_settings.HttpPort, name: "http"), },
                                        LivenessProbe = new V1Probe(
                                            timeoutSeconds: 1,
                                            initialDelaySeconds: 30,
                                            httpGet: new V1HTTPGetAction("http", path: "/health")),
                                        ReadinessProbe = new V1Probe(
                                            timeoutSeconds: 1,
                                            initialDelaySeconds: 15,
                                            httpGet: new V1HTTPGetAction("http", path: "/ready")),
                                        Resources = new V1ResourceRequirements
                                        {
                                            Requests = new Dictionary<string, ResourceQuantity>
                                            {
                                                { "cpu", new ResourceQuantity("100m") },
                                                { "memory", new ResourceQuantity("64Mi") },
                                            },
                                            Limits = new Dictionary<string, ResourceQuantity>
                                            {
                                                { "cpu", new ResourceQuantity("100m") },
                                                { "memory", new ResourceQuantity("128Mi") },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    }),
                Format));

        await fileWriter.OutputAsync(OutputPath);
        return ExitCodes.Success;
    }
}
