﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using k8s.Models;
using KubeOps.Operator.Entities.Kustomize;
using KubeOps.Operator.Rbac;
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

        public static string OperatorName(Assembly? assembly) => OperatorSpec(assembly) ?.Name ?? "operator";

        private static OperatorSpecAttribute? OperatorSpec(Assembly? assembly) => assembly?.GetCustomAttribute<OperatorSpecAttribute>();

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
           

            var output = _serializer.Serialize(GenerateDeployment(Assembly.GetEntryAssembly()), Format);

            if (!string.IsNullOrWhiteSpace(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
                await using var file = File.Open(Path.Join(OutputPath,
                    $"deployment.{Format.ToString().ToLower()}"), FileMode.Create);

                await file.WriteAsync(Encoding.UTF8.GetBytes(output));

                var kustomize = new KustomizationConfig
                {
                    Resources = new List<string> { $"deployment.{Format.ToString().ToLower()}" },
                    CommonLabels = new Dictionary<string, string>
                    {
                        {"operator-element", "operator-instance"},
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

        public static V1Deployment GenerateDeployment(Assembly? assembly)
        {
            if (assembly == null)
            {
                throw new Exception("No Entry Assembly found.");
            }

            var spec = OperatorSpec(assembly);
            var name = spec?.Name ?? "operator";
            var containerRegistry = spec?.ContainerRegistry;

            var secrets = spec?.ImagePullSecretName != null ? new List<V1LocalObjectReference>
            {
                new V1LocalObjectReference { Name = spec.ImagePullSecretName }
            } : null;

            return new V1Deployment(
                            $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                            V1Deployment.KubeKind,
                            new V1ObjectMeta(name: name),
                            new V1DeploymentSpec
                            {
                                Replicas = 1,
                                RevisionHistoryLimit = 0,
                                Template = new V1PodTemplateSpec
                                {
                                    Spec = new V1PodSpec
                                    {
                                        TerminationGracePeriodSeconds = 10,
                                        NodeSelector = new Dictionary<string, string> { { "beta.kubernetes.io/os", "linux" } },
                                        ImagePullSecrets = secrets,
                                        Containers = new List<V1Container>
                                        {
                                new V1Container
                                {
                                    Image = $"{(string.IsNullOrEmpty(containerRegistry) ? "" : containerRegistry + "/")}{name}",
                                    Name = "operator", // this name is just the container name, which makes sense to be a fixed string
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<string, ResourceQuantity>
                                        {
                                            {"cpu", new ResourceQuantity("100m")},
                                            {"memory", new ResourceQuantity("64Mi")},
                                        },
                                        Limits = new Dictionary<string, ResourceQuantity>
                                        {
                                            {"cpu", new ResourceQuantity("100m")},
                                            {"memory", new ResourceQuantity("128Mi")},
                                        },
                                    },
                                },
                                        },
                                    },
                                },
                            });
        }
    }
}
