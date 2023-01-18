using k8s.Models;
using KubeOps.KubernetesClient.Entities;

namespace KubeOps.Operator.Rbac;

internal static class RbacAttributeExtensions
{
    public static IEnumerable<V1PolicyRule> CreateRbacPolicies(this IEnumerable<EntityRbacAttribute> attributes)
    {
        var attributeList = attributes.ToList();

        return attributeList.CreateEntityPolicies().Concat(attributeList.CreateEntityStatusPolicies());
    }

    public static V1PolicyRule CreateRbacPolicy(this GenericRbacAttribute attribute) => new()
    {
        ApiGroups = attribute.Groups,
        Resources = attribute.Resources,
        NonResourceURLs = attribute.Urls,
        Verbs = attribute.Verbs.ConvertToStrings(),
    };

    private static IEnumerable<V1PolicyRule> CreateEntityPolicies(this IEnumerable<EntityRbacAttribute> attributes)
        => attributes
            .SelectMany(attribute => attribute.Entities.Select(type => (EntityType: type, attribute.Verbs)))
            .GroupBy(e => e.EntityType)
            .Select(
                group => (
                    Crd: group.Key.ToEntityDefinition(),
                    Verbs: group.Aggregate(RbacVerb.None, (accumulator, element) => accumulator | element.Verbs)))
            .GroupBy(group => group.Verbs)
            .Select(
                group => (
                    Verbs: group.Key,
                    Crds: group.Select(element => element.Crd).ToList()))
            .Select(
                group => new V1PolicyRule
                {
                    ApiGroups = group.Crds.Select(crd => crd.Group).Distinct().ToList(),
                    Resources = group.Crds.Select(crd => crd.Plural).Distinct().ToList(),
                    Verbs = group.Verbs.ConvertToStrings(),
                });

    private static IEnumerable<V1PolicyRule> CreateEntityStatusPolicies(
        this IEnumerable<EntityRbacAttribute> attributes)
        => attributes
            .SelectMany(attribute => attribute.Entities.Select(type => (EntityType: type, attribute.Verbs)))
            .Where(e => e.EntityType.GetProperty("Status") != null)
            .GroupBy(e => e.EntityType)
            .Select(group => group.Key.ToEntityDefinition())
            .Select(
                crd => new V1PolicyRule()
                {
                    ApiGroups = new[] { crd.Group },
                    Resources = new[] { $"{crd.Plural}/status" },
                    Verbs = (RbacVerb.Get | RbacVerb.Patch | RbacVerb.Update).ConvertToStrings(),
                });

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
