using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    [Command("rbac", Description = "Generates the needed rbac roles for the operator.")]
    internal class RbacGenerator : GeneratorBase
    {
        private readonly EntitySerializer _serializer;

        public RbacGenerator(EntitySerializer serializer)
        {
            _serializer = serializer;
        }

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var output = _serializer.Serialize(GenerateManagerRbac(), Format);

            if (!string.IsNullOrWhiteSpace(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
                await using var file = File.Open(Path.Join(OutputPath,
                    $"operator.{Format.ToString().ToLower()}"), FileMode.Create);
                await file.WriteAsync(Encoding.UTF8.GetBytes(output));

                var kustomize = new KustomizationConfig
                {
                    Resources = new List<string> {$"operator.{Format.ToString().ToLower()}"},
                    CommonLabels = new Dictionary<string, string>
                    {
                        {"operator-element", "rbac"},
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

        public static V1ClusterRole GenerateManagerRbac()
        {
            var assembly = Assembly.GetEntryAssembly();
            if (assembly == null)
            {
                throw new Exception("No Entry Assembly found.");
            }

            return new V1ClusterRole(
                null,
                $"{V1ClusterRole.KubeGroup}/{V1ClusterRole.KubeApiVersion}",
                V1ClusterRole.KubeKind,
                new V1ObjectMeta {Name = "operator-role"},
                new List<V1PolicyRule>(
                    GetAttributes<EntityRbacAttribute>(assembly)
                        .SelectMany(a => a.CreateRbacPolicies())
                        .Concat(GetAttributes<GenericRbacAttribute>(assembly).Select(a => a.CreateRbacPolicy()))
                ));
        }

        private static IEnumerable<TAttribute> GetAttributes<TAttribute>(Assembly assembly)
            where TAttribute : Attribute =>
            assembly.GetTypes().SelectMany(t => t.GetCustomAttributes<TAttribute>(true));
    }
}
