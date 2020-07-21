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
            var assembly = Assembly.GetEntryAssembly();
            var rbacRole = GetRbacRole(assembly);

            var role = _serializer.Serialize(GenerateManagerRbac(assembly, rbacRole), Format);
            var roleBinding = _serializer.Serialize(GenerateRoleBinding(rbacRole), Format);

            if (!string.IsNullOrWhiteSpace(OutputPath))
            {


                Directory.CreateDirectory(OutputPath);
                await using var roleFile = File.Open(Path.Join(OutputPath,
                    $"{rbacRole}.{Format.ToString().ToLower()}"), FileMode.Create);
                await roleFile.WriteAsync(Encoding.UTF8.GetBytes(role));
                await using var bindingFile = File.Open(Path.Join(OutputPath,
                    $"{rbacRole}-binding.{Format.ToString().ToLower()}"), FileMode.Create);
                await bindingFile.WriteAsync(Encoding.UTF8.GetBytes(roleBinding));

                var kustomize = new KustomizationConfig
                {
                    Resources = new List<string>
                    {
                        $"{rbacRole}.{Format.ToString().ToLower()}",
                        $"{rbacRole}-binding.{Format.ToString().ToLower()}",
                    },
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
                await app.Out.WriteLineAsync(role);
                await app.Out.WriteLineAsync(roleBinding);
            }

            return ExitCodes.Success;
        }

        public static string GetRbacRole(Assembly? assembly) => 
            assembly?.GetCustomAttribute<RbacRoleAttribute>()?.Prefix 
            ?? OperatorGenerator.OperatorName(assembly) + "-role";

        public static V1ClusterRoleBinding GenerateRoleBinding(string rbacRole)
        {
            return new V1ClusterRoleBinding
            {
                ApiVersion = $"{V1ClusterRoleBinding.KubeGroup}/{V1ClusterRoleBinding.KubeApiVersion}",
                Kind = V1ClusterRoleBinding.KubeKind,
                Metadata = new V1ObjectMeta { Name = $"{rbacRole}-binding" },
                RoleRef = new V1RoleRef(V1ClusterRole.KubeGroup, V1ClusterRole.KubeKind, rbacRole),
                Subjects = new List<V1Subject>
                {
                    new V1Subject(V1ServiceAccount.KubeKind, "default", namespaceProperty: "system")
                }
            };
        }

        public static V1ClusterRole GenerateManagerRbac(Assembly? assembly, string rbacRole)
        {
            if (assembly == null)
            {
                throw new Exception("No Entry Assembly found.");
            }
            

            return new V1ClusterRole(
                null,
                $"{V1ClusterRole.KubeGroup}/{V1ClusterRole.KubeApiVersion}",
                V1ClusterRole.KubeKind,
                new V1ObjectMeta { Name = rbacRole },
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
