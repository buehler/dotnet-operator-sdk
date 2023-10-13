using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Kustomize;
using KubeOps.Cli.Certificates;
using KubeOps.Cli.Output;
using KubeOps.Cli.Transpilation;
using KubeOps.Transpiler;

using Spectre.Console;

namespace KubeOps.Cli.Commands.Generator;

internal static class WebhookOperatorGenerator
{
    public static Command Command
    {
        get
        {
            var cmd = new Command(
                "webhook-operator",
                "Generates deployments and other resources for an operator with webhooks to run.")
            {
                Options.OutputFormat,
                Options.OutputPath,
                Options.SolutionProjectRegex,
                Options.TargetFramework,
                Arguments.OperatorName,
                Arguments.SolutionOrProjectFile,
            };
            cmd.AddAlias("wh-op");
            cmd.SetHandler(ctx => Handler(AnsiConsole.Console, ctx));

            return cmd;
        }
    }

    internal static async Task Handler(IAnsiConsole console, InvocationContext ctx)
    {
        var name = ctx.ParseResult.GetValueForArgument(Arguments.OperatorName);
        var file = ctx.ParseResult.GetValueForArgument(Arguments.SolutionOrProjectFile);
        var outPath = ctx.ParseResult.GetValueForOption(Options.OutputPath);
        var format = ctx.ParseResult.GetValueForOption(Options.OutputFormat);

        var result = new ResultOutput(console, format);
        console.WriteLine("Generate webhook resources.");

        console.MarkupLine("Generate [cyan]CA[/] certificate and private key.");
        var (caCert, caKey) = Certificates.CertificateGenerator.CreateCaCertificate();

        result.Add("ca.pem", caCert.ToPem(), OutputFormat.Plain);
        result.Add("ca-key.pem", caKey.ToPem(), OutputFormat.Plain);

        console.MarkupLine("Generate [cyan]server[/] certificate and private key.");
        var (srvCert, srvKey) = Certificates.CertificateGenerator.CreateServerCertificate(
            (caCert, caKey),
            name,
            $"{name}-system");

        result.Add("svc.pem", srvCert.ToPem(), OutputFormat.Plain);
        result.Add("svc-key.pem", srvKey.ToPem(), OutputFormat.Plain);

        console.MarkupLine("Generate [cyan]deployment[/].");
        result.Add(
            $"deployment.{format.ToString().ToLowerInvariant()}",
            new V1Deployment(
                metadata: new V1ObjectMeta(
                    labels: new Dictionary<string, string> { { "operator-deployment", "kubernetes-operator" } },
                    name: "operator"),
                spec: new V1DeploymentSpec
                {
                    Replicas = 1,
                    RevisionHistoryLimit = 0,
                    Selector = new V1LabelSelector(
                        matchLabels:
                        new Dictionary<string, string> { { "operator-deployment", "kubernetes-operator" } }),
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta(
                            labels:
                            new Dictionary<string, string> { { "operator-deployment", "kubernetes-operator" }, }),
                        Spec = new V1PodSpec
                        {
                            TerminationGracePeriodSeconds = 10,
                            Volumes = new List<V1Volume>
                            {
                                new() { Name = "certificates", Secret = new() { SecretName = "webhook-cert" }, },
                                new() { Name = "ca-certificates", Secret = new() { SecretName = "webhook-ca" }, },
                            },
                            Containers = new List<V1Container>
                            {
                                new()
                                {
                                    Image = "operator",
                                    Name = "operator",
                                    VolumeMounts = new List<V1VolumeMount>
                                    {
                                        new()
                                        {
                                            Name = "certificates",
                                            MountPath = "/certs",
                                            ReadOnlyProperty = true,
                                        },
                                        new()
                                        {
                                            Name = "ca-certificates",
                                            MountPath = "/ca",
                                            ReadOnlyProperty = true,
                                        },
                                    },
                                    Env = new List<V1EnvVar>
                                    {
                                        new()
                                        {
                                            Name = "POD_NAMESPACE",
                                            ValueFrom =
                                                new V1EnvVarSource
                                                {
                                                    FieldRef = new V1ObjectFieldSelector
                                                    {
                                                        FieldPath = "metadata.namespace",
                                                    },
                                                },
                                        },
                                    },
                                    EnvFrom =
                                        new List<V1EnvFromSource>
                                        {
                                            new() { ConfigMapRef = new() { Name = "webhook-config" } },
                                        },
                                    Ports = new List<V1ContainerPort> { new(5001, name: "https"), },
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
                }).Initialize());

        console.MarkupLine("Generate [cyan]service[/].");
        result.Add(
            $"service.{format.ToString().ToLowerInvariant()}",
            new V1Service(
                metadata: new V1ObjectMeta(name: "operator"),
                spec: new V1ServiceSpec
                {
                    Ports =
                        new List<V1ServicePort> { new() { Name = "https", TargetPort = "https", Port = 443, }, },
                    Selector = new Dictionary<string, string> { { "operator-deployment", "kubernetes-operator" }, },
                }).Initialize());

        console.MarkupLine("Generate [cyan]webhook configurations[/].");
        var parser = file switch
        {
            { Extension: ".csproj", Exists: true } => await AssemblyLoader.ForProject(console, file),
            { Extension: ".sln", Exists: true } => await AssemblyLoader.ForSolution(
                console,
                file,
                ctx.ParseResult.GetValueForOption(Options.SolutionProjectRegex),
                ctx.ParseResult.GetValueForOption(Options.TargetFramework)),
            { Exists: false } => throw new FileNotFoundException($"The file {file.Name} does not exist."),
            _ => throw new NotSupportedException("Only *.csproj and *.sln files are supported."),
        };
        var validatedEntities = parser.GetValidatedEntities().ToList();
        var validatorConfig = new V1ValidatingWebhookConfiguration(
            metadata: new V1ObjectMeta(name: "validators"),
            webhooks: new List<V1ValidatingWebhook>()).Initialize();

        foreach (var entity in validatedEntities)
        {
            validatorConfig.Webhooks.Add(new V1ValidatingWebhook
            {
                Name = $"validate.{entity.Metadata.SingularName}.{entity.Metadata.Group}.{entity.Metadata.Version}",
                MatchPolicy = "Exact",
                AdmissionReviewVersions = new[] { "v1" },
                SideEffects = "None",
                Rules = new[]
                {
                    new V1RuleWithOperations
                    {
                        Operations = entity.GetOperations(),
                        Resources = new[] { entity.Metadata.PluralName },
                        ApiGroups = new[] { entity.Metadata.Group },
                        ApiVersions = new[] { entity.Metadata.Version },
                    },
                },
                ClientConfig = new Admissionregistrationv1WebhookClientConfig
                {
                    CaBundle =
                        Encoding.ASCII.GetBytes(Convert.ToBase64String(Encoding.ASCII.GetBytes(caCert.ToPem()))),
                    Service = new Admissionregistrationv1ServiceReference
                    {
                        Name = "operator", Path = entity.ValidatorPath,
                    },
                },
            });
        }

        if (validatedEntities.Any())
        {
            result.Add(
                $"validators.{format.ToString().ToLowerInvariant()}", validatorConfig);
        }

        result.Add(
            $"kustomization.{format.ToString().ToLowerInvariant()}",
            new KustomizationConfig
            {
                Resources =
                    new List<string>
                    {
                        $"deployment.{format.ToString().ToLowerInvariant()}",
                        $"service.{format.ToString().ToLowerInvariant()}",
                        validatorConfig.Webhooks.Any()
                            ? $"validators.{format.ToString().ToLowerInvariant()}"
                            : string.Empty,
                    }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList(),
                CommonLabels = new Dictionary<string, string> { { "operator-element", "operator-instance" }, },
                ConfigMapGenerator = new List<KustomizationConfigMapGenerator>
                {
                    new()
                    {
                        Name = "webhook-config",
                        Literals = new List<string>
                        {
                            "KESTREL__ENDPOINTS__HTTP__URL=http://0.0.0.0:5000",
                            "KESTREL__ENDPOINTS__HTTPS__URL=https://0.0.0.0:5001",
                            "KESTREL__ENDPOINTS__HTTPS__CERTIFICATE__PATH=/certs/svc.pem",
                            "KESTREL__ENDPOINTS__HTTPS__CERTIFICATE__KEYPATH=/certs/svc-key.pem",
                        },
                    },
                },
                SecretGenerator = new List<KustomizationSecretGenerator>
                {
                    new() { Name = "webhook-ca", Files = new List<string> { "ca.pem", "ca-key.pem", }, },
                    new() { Name = "webhook-cert", Files = new List<string> { "svc.pem", "svc-key.pem", }, },
                },
            });

        if (outPath is not null)
        {
            await result.Write(outPath);
        }
        else
        {
            result.Write();
        }
    }
}
