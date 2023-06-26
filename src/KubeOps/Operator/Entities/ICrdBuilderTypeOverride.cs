using k8s.Models;

namespace KubeOps.Operator.Entities;

/// <summary>
/// The override definition class which sets the condition for which the type should be overridden during CRD generation,
/// and what should the serialized values map to.
/// </summary>
public interface ICrdBuilderTypeOverride
{
    /// <summary>
    /// Checks if the type matches the user defined condition that will custom configure the schema property for the given type.
    /// </summary>
    /// <param name="type">Type being checked against.</param>
    /// <returns>Boolean determining whether the user defined type condition has been matched.</returns>
    public bool HandlesType(Type type);

    /// <summary>
    /// For the matching condition, configure the CRD property in a user defined way for the given type.
    /// </summary>
    /// <param name="props">The object type that will be converted to a schema.</param>
    public void ConfigureCustomSchemaForProp(V1JSONSchemaProps props);
}
