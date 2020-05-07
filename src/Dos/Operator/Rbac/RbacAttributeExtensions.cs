using System.Collections.Generic;
using System.Linq;
using Dos.Operator.Entities;
using k8s.Models;

namespace Dos.Operator.Rbac
{
    internal static class RbacAttributeExtensions
    {
        public static IEnumerable<V1PolicyRule> CreateRbacPolicies(this EntityRbacAttribute attribute) =>
            new[]
                {
                    attribute.CreateRbacPolicy(),
                    attribute.CreateStatusRbacPolicy(),
                }
                .Where(p => p != null)
                .ToList() as List<V1PolicyRule>;

        public static V1PolicyRule CreateRbacPolicy(this EntityRbacAttribute attribute)
        {
            var crds = attribute.Entities.Select(EntityExtensions.CreateResourceDefinition).ToList();
            var policy = new V1PolicyRule
            {
                ApiGroups = crds.Select(crd => crd.Group).Distinct().ToList(),
                Resources = crds.Select(crd => crd.Plural).Distinct().ToList(),
                Verbs = attribute.Verbs.ConvertToStrings(),
            };

            return policy;
        }

        public static V1PolicyRule? CreateStatusRbacPolicy(this EntityRbacAttribute attribute)
        {
            var crds = attribute.Entities
                .Where(type => type.GetProperty("Status") != null)
                .Select(EntityExtensions.CreateResourceDefinition)
                .ToList();
            if (crds.Count == 0)
            {
                return null;
            }

            var policy = new V1PolicyRule
            {
                ApiGroups = crds.Select(crd => crd.Group).Distinct().ToList(),
                Resources = crds.Select(crd => crd.Plural).Distinct().Select(name => $"{name}/status").ToList(),
                Verbs = (RbacVerb.Get | RbacVerb.Patch | RbacVerb.Update).ConvertToStrings(),
            };

            return policy;
        }

        public static V1PolicyRule CreateRbacPolicy(this GenericRbacAttribute attribute) => new V1PolicyRule
        {
            ApiGroups = attribute.Groups,
            Resources = attribute.Resources,
            NonResourceURLs = attribute.Urls,
            Verbs = attribute.Verbs.ConvertToStrings(),
        };

        private static IList<string> ConvertToStrings(this RbacVerb verbs)
        {
            if (verbs == RbacVerb.None)
            {
                return new List<string>();
            }

            if (verbs.HasFlag(RbacVerb.All))
            {
                return new List<string> { "*" };
            }

            var result = new List<string>();

            if (verbs.HasFlag(RbacVerb.Create))
            {
                result.Add("create");
            }

            if (verbs.HasFlag(RbacVerb.Get))
            {
                result.Add("get");
            }

            if (verbs.HasFlag(RbacVerb.List))
            {
                result.Add("list");
            }

            if (verbs.HasFlag(RbacVerb.Watch))
            {
                result.Add("watch");
            }

            if (verbs.HasFlag(RbacVerb.Patch))
            {
                result.Add("patch");
            }

            if (verbs.HasFlag(RbacVerb.Update))
            {
                result.Add("update");
            }

            if (verbs.HasFlag(RbacVerb.Delete))
            {
                result.Add("delete");
            }

            return result;
        }
    }
}
