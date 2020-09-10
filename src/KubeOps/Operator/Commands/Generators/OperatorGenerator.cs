using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using k8s.Models;
using KubeOps.Operator.Entities.Kustomize;
using KubeOps.Operator.Serialization;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators
{
    [Command("operator", Description = "Generates the needed yamls to run the operator.")]
    internal class OperatorGenerator : GeneratorBase
    {
        private readonly EntitySerializer _serializer;

        public OperatorGenerator(EntitySerializer serializer)
        {
            _serializer = serializer;
        }

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var output = _serializer.Serialize(
                new V1Deployment(
                    $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                    V1Deployment.KubeKind,
                    new V1ObjectMeta(name: "operator"),
                    new V1DeploymentSpec
                    {
                        Replicas = 1,
                        RevisionHistoryLimit = 0,
                        Template = new V1PodTemplateSpec
                        {
                            Spec = new V1PodSpec
                            {
                                TerminationGracePeriodSeconds = 10,
                                Containers = new List<V1Container>
                                {
                                    new V1Container
                                    {
                                        Image = "operator",
                                        Name = "operator",
                                        Ports = new List<V1ContainerPort>
                                        {
                                            new V1ContainerPort(80, name: "http"),
                                        },
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
                Format);

            if (!string.IsNullOrWhiteSpace(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
                await using var file = File.Open(
                    Path.Join(
                        OutputPath,
                        $"deployment.{Format.ToString().ToLower()}"),
                    FileMode.Create);

                await file.WriteAsync(Encoding.UTF8.GetBytes(output));

                var kustomize = new KustomizationConfig
                {
                    Resources = new List<string> { $"deployment.{Format.ToString().ToLower()}" },
                    CommonLabels = new Dictionary<string, string>
                    {
                        { "operator-element", "operator-instance" },
                    },
                };
                var kustomizeOutput = Encoding.UTF8.GetBytes(_serializer.Serialize(kustomize, Format));
                await using var kustomizationFile =
                    File.Open(Path.Join(OutputPath, $"kustomization.{Format.ToString().ToLower()}"), FileMode.Create);
                await kustomizationFile.WriteAsync(kustomizeOutput);
            }
            else
            {
                await app.Out.WriteLineAsync(output);
            }

            return ExitCodes.Success;
        }
    }
}
