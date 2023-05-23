namespace KubeOps.Operator.Entities;

internal class CrdBuilderTypeOverrides : ICrdBuilderTypeOverrides
{
    public CrdBuilderTypeOverrides(IEnumerable<ICrdBuilderTypeOverride> typeOverrides)
    {
        TypeOverrides = typeOverrides;
    }

    private IEnumerable<ICrdBuilderTypeOverride> TypeOverrides { get; }

    public ICrdBuilderTypeOverride? GetMatchingTypeOverride(Type type, string? jsonPath) =>

        // First check if jsonPaths match. Should only ever return single or null
        TypeOverrides
            .SingleOrDefault(ovrd => ovrd.TargetJsonPath == jsonPath && ovrd.TargetJsonPath != null)

        // If none of the jsonPaths matched, then match by type only for objects where jsonPath has not been defined
        ?? TypeOverrides
            .SingleOrDefault(ovrd => ovrd.TypeMatchesOverrideCondition(type) && ovrd.TargetJsonPath == null);
}
