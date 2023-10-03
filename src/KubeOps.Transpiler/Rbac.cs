using k8s.Models;

using KubeOps.Abstractions.Rbac;

namespace KubeOps.Transpiler;

/// <summary>
/// Class for the conversion of <see cref="RbacAttribute"/>s to <see cref="V1PolicyRule"/>s.
/// </summary>
public static class Rbac
{
    /// <summary>
    /// Convert a list of <see cref="RbacAttribute"/>s to a list of <see cref="V1PolicyRule"/>s.
    /// The rules are grouped by entity type and verbs.
    /// </summary>
    /// <param name="attributes">List of <see cref="RbacAttribute"/>s.</param>
    /// <returns>A converted, grouped list of <see cref="V1PolicyRule"/>s.</returns>
    public static IEnumerable<V1PolicyRule> Transpile(IEnumerable<RbacAttribute> attributes)
    {
        var list = attributes.ToList();

        var generic = list
            .OfType<GenericRbacAttribute>()
            .Select(a => new V1PolicyRule
            {
                ApiGroups = a.Groups,
                Resources = a.Resources,
                NonResourceURLs = a.Urls,
                Verbs = ConvertToStrings(a.Verbs),
            });

        var entities = list
            .OfType<EntityRbacAttribute>()
            .SelectMany(attribute =>
                attribute.Entities.Select(type => (EntityType: type, attribute.Verbs)))
            .GroupBy(e => e.EntityType)
            .Select(
                group => (
                    Crd: Entities.ToEntityMetadata(group.Key),
                    Verbs: group.Aggregate(RbacVerb.None, (accumulator, element) => accumulator | element.Verbs)))
            .GroupBy(group => group.Verbs)
            .Select(
                group => (
                    Verbs: group.Key,
                    Crds: group.Select(element => element.Crd).ToList()))
            .Select(
                group => new V1PolicyRule
                {
                    ApiGroups = group.Crds.Select(crd => crd.Metadata.Group).Distinct().ToList(),
                    Resources = group.Crds.Select(crd => crd.Metadata.PluralName).Distinct().ToList(),
                    Verbs = ConvertToStrings(group.Verbs),
                });

        var entityStatus = list
            .OfType<EntityRbacAttribute>()
            .SelectMany(attribute => attribute.Entities.Select(type => (EntityType: type, attribute.Verbs)))
            .Where(e => e.EntityType.GetProperty("Status") != null)
            .GroupBy(e => e.EntityType)
            .Select(group => Entities.ToEntityMetadata(group.Key))
            .Select(
                crd => new V1PolicyRule()
                {
                    ApiGroups = new[] { crd.Metadata.Group },
                    Resources = new[] { $"{crd.Metadata.PluralName}/status" },
                    Verbs = ConvertToStrings(RbacVerb.Get | RbacVerb.Patch | RbacVerb.Update),
                });

        return generic.Concat(entities).Concat(entityStatus);
    }

    private static string[] ConvertToStrings(RbacVerb verbs) => verbs switch
    {
        RbacVerb.None => Array.Empty<string>(),
        _ when verbs.HasFlag(RbacVerb.All) => new[] { "*" },
        _ =>
#if NETSTANDARD
            Enum.GetValues(typeof(RbacVerb)).Cast<RbacVerb>()
#else
            Enum.GetValues<RbacVerb>()
#endif
                .Where(v => verbs.HasFlag(v) && v != RbacVerb.All && v != RbacVerb.None)
                .Select(v => v.ToString().ToLowerInvariant())
                .ToArray(),
    };
}
