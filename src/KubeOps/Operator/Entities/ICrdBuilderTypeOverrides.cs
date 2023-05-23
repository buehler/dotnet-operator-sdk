using k8s.Models;

namespace KubeOps.Operator.Entities;

/// <summary>
/// Interface representing a set of type overrides for CRD generation.
/// </summary>
public interface ICrdBuilderTypeOverrides
{
    /// <summary>
    /// Returns the override condition/action pair that matches user defined condition.
    /// </summary>
    /// <param name="type">Type being checked against.</param>
    /// <param name="jsonpath">The specific schema jsonpath being checked against.</param>
    /// <returns>The override containing a possible JsonPath, type match condition, and action on the property.</returns>
    public ICrdBuilderTypeOverride? GetMatchingTypeOverride(Type type, string jsonpath);
}

/// <summary>
/// The override definition class which sets the condition for which the type should be overridden during CRD generation,
/// and what should the serialized values map to.
/// </summary>
public interface ICrdBuilderTypeOverride
{
    /// <summary>
    /// Target JSON path on the CRD schema.
    /// </summary>
    public string? TargetJsonPath { get; }

    /// <summary>
    /// Checks if the type matches the user defined condition that will custom configure the schema property for the given type.
    /// </summary>
    /// <param name="type">Type being checked against.</param>
    /// <returns>Boolean determining whether the user defined type condition has been matched.</returns>
    public bool TypeMatchesOverrideCondition(Type type);

    /// <summary>
    /// For the matching condition, configure the CRD property in a user defined way for the given type.
    /// </summary>
    /// <param name="props">The object type that will be converted to a schema.</param>
    public void ConfigureCustomSchemaForProp(V1JSONSchemaProps props);
}
