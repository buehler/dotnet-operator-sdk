using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using k8s.Models;
using KubeOps.Operator.Services;

namespace KubeOps.Operator.Rbac
{
    internal class RbacBuilder : IRbacBuilder
    {
        private readonly List<Type> _componentTypes;

        private readonly bool _hasWebhooks;

        public RbacBuilder(
            IEnumerable<ControllerType> controllers,
            IEnumerable<FinalizerType> finalizers,
            IEnumerable<ValidatorType> validators,
            IEnumerable<MutatorType> mutators)
        {
            controllers = controllers.ToList();
            finalizers = finalizers.ToList();
            validators = validators.ToList();
            mutators = mutators.ToList();

            _componentTypes = Enumerable.Empty<Type>()
                .Concat(controllers.Select(c => c.InstanceType))
                .Concat(finalizers.Select(c => c.InstanceType))
                .Concat(validators.Select(c => c.InstanceType))
                .Concat(mutators.Select(c => c.InstanceType))
                .Distinct()
                .ToList();

            _hasWebhooks = validators.Any() || mutators.Any();
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
