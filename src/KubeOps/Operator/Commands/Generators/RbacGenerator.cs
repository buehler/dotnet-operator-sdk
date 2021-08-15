using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s.Models;
using KubeOps.Operator.Commands.CommandHelpers;
using KubeOps.Operator.Entities.Kustomize;
using KubeOps.Operator.Rbac;
using KubeOps.Operator.Serialization;
using KubeOps.Operator.Services;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator.Commands.Generators
{
    [Command("rbac", Description = "Generates the needed rbac roles for the operator.")]
    internal class RbacGenerator : GeneratorBase
    {
        private readonly EntitySerializer _serializer;
        private readonly ResourceLocator _resourceLocator;
        private readonly bool _hasWebhooks;

        public RbacGenerator(
            EntitySerializer serializer,
            ResourceLocator resourceLocator,
            IEnumerable<ValidatorType> validators)
        {
            _serializer = serializer;
            _resourceLocator = resourceLocator;

            // TODO Consider Mutating Webhooks
            _hasWebhooks = validators.Any();
        }

        // TODO Remove ResourceLocator
        public V1ClusterRole GenerateManagerRbac(ResourceLocator resourceTypeService)
        {
            var entityRbacPolicyRules = resourceTypeService.GetAttributes<EntityRbacAttribute>()
                .SelectMany(attribute => attribute.CreateRbacPolicies());

            var genericRbacPolicyRules = resourceTypeService.GetAttributes<GenericRbacAttribute>()
                .Select(attribute => attribute.CreateRbacPolicy());

            var rules = entityRbacPolicyRules.Concat(genericRbacPolicyRules).ToList();

            if (_hasWebhooks)
            {
                var servicePolicies = new EntityRbacAttribute(
                    typeof(V1Service),
                    typeof(V1ValidatingWebhookConfiguration))
                {
                    Verbs = RbacVerb.Get | RbacVerb.Create | RbacVerb.Update | RbacVerb.Patch,
                }.CreateRbacPolicies();

                rules = rules.Concat(servicePolicies).ToList();
            }

            return new V1ClusterRole(
                null,
                $"{V1ClusterRole.KubeGroup}/{V1ClusterRole.KubeApiVersion}",
                V1ClusterRole.KubeKind,
                new V1ObjectMeta { Name = "operator-role" },
                new List<V1PolicyRule>(rules));
        }

        public async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var fileWriter = new FileWriter(app.Out);
            fileWriter.Add(
                $"operator-role.{Format.ToString().ToLower()}",
                _serializer.Serialize(GenerateManagerRbac(_resourceLocator), Format));
            fileWriter.Add(
                $"operator-role-binding.{Format.ToString().ToLower()}",
                _serializer.Serialize(
                    new V1ClusterRoleBinding
                    {
                        ApiVersion = $"{V1ClusterRoleBinding.KubeGroup}/{V1ClusterRoleBinding.KubeApiVersion}",
                        Kind = V1ClusterRoleBinding.KubeKind,
                        Metadata = new V1ObjectMeta { Name = "operator-role-binding" },
                        RoleRef = new V1RoleRef(V1ClusterRole.KubeGroup, V1ClusterRole.KubeKind, "operator-role"),
                        Subjects = new List<V1Subject>
                        {
                            new(V1ServiceAccount.KubeKind, "default", namespaceProperty: "system"),
                        },
                    },
                    Format));
            fileWriter.Add(
                $"kustomization.{Format.ToString().ToLower()}",
                _serializer.Serialize(
                    new KustomizationConfig
                    {
                        Resources = new List<string>
                        {
                            $"operator-role.{Format.ToString().ToLower()}",
                            $"operator-role-binding.{Format.ToString().ToLower()}",
                        },
                        CommonLabels = new Dictionary<string, string>
                        {
                            { "operator-element", "rbac" },
                        },
                    },
                    Format));

            await fileWriter.OutputAsync(OutputPath);
            return ExitCodes.Success;
        }
    }
}
