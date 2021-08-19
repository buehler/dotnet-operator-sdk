using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using k8s.Models;
using KubeOps.Operator.Builder;

namespace KubeOps.Operator.Rbac
{
    internal class RbacBuilder : IRbacBuilder
    {
        private readonly List<Type> _componentTypes;

        private readonly bool _hasWebhooks;
        private readonly bool _hasLeaderElection;

        public RbacBuilder(IComponentRegistrar componentRegistrar, OperatorSettings settings)
        {
            var controllerTypes = componentRegistrar.ControllerRegistrations
                .Select(t => t.ControllerType)
                .ToList();
            var finalizerTypes = componentRegistrar.FinalizerRegistrations
                .Select(t => t.FinalizerType)
                .ToList();
            var validatorTypes = componentRegistrar.ValidatorRegistrations
                .Select(t => t.ValidatorType)
                .ToList();
            var mutatorTypes = componentRegistrar.MutatorRegistrations
                .Select(t => t.MutatorType)
                .ToList();
            var entityTypes = componentRegistrar.EntityRegistrations
                .Select(t => t.EntityType)
                .ToList();

            _componentTypes = Enumerable.Empty<Type>()
                .Concat(controllerTypes)
                .Concat(finalizerTypes)
                .Concat(validatorTypes)
                .Concat(mutatorTypes)
                .Concat(entityTypes)
                .Distinct()
                .ToList();

            _hasWebhooks = validatorTypes.Any() || mutatorTypes.Any();
            _hasLeaderElection = settings.EnableLeaderElection;
        }

        public V1ClusterRole BuildManagerRbac()
        {
            var entityRbacPolicyRules = GetAttributes<EntityRbacAttribute>()
                .SelectMany(attribute => attribute.CreateRbacPolicies());

            var genericRbacPolicyRules = GetAttributes<GenericRbacAttribute>()
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

            if (_hasLeaderElection)
            {
                rules = rules
                    .Concat(new EntityRbacAttribute(typeof(V1Lease)) { Verbs = RbacVerb.All }.CreateRbacPolicies())
                    .Concat(
                        new EntityRbacAttribute(typeof(V1Deployment)) { Verbs = RbacVerb.Get | RbacVerb.List }
                            .CreateRbacPolicies())
                    .ToList();
            }

            return new V1ClusterRole(
                null,
                $"{V1ClusterRole.KubeGroup}/{V1ClusterRole.KubeApiVersion}",
                V1ClusterRole.KubeKind,
                new V1ObjectMeta { Name = "operator-role" },
                new List<V1PolicyRule>(rules));
        }

        private IEnumerable<TAttribute> GetAttributes<TAttribute>()
            where TAttribute : Attribute =>
            _componentTypes.SelectMany(type => type.GetCustomAttributes<TAttribute>(true));
    }
}
