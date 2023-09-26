using System.Reflection;

using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace KubeOps.Transpiler;

/// <summary>
/// Class for the conversion of types to <see cref="EntityMetadata"/> objects.
/// </summary>
public static class Entities
{
    /// <summary>
    /// Converts the given type to an <see cref="EntityMetadata"/> object.
    /// </summary>
    /// <param name="entityType">The type to convert.</param>
    /// <returns>The <see cref="EntityMetadata"/> object representing the type with the scope of the entity.</returns>
    /// <exception cref="ArgumentException">Thrown if the given type is not a valid Kubernetes entity.</exception>
    public static (EntityMetadata Metadata, string Scope) ToEntityMetadata(Type entityType)
        => (entityType.GetCustomAttribute<KubernetesEntityAttribute>(),
                entityType.GetCustomAttribute<EntityScopeAttribute>()) switch
        {
            (null, _) => throw new ArgumentException("The given type is not a valid Kubernetes entity."),
            ({ } attr, var scope) => (new(
                    Defaulted(attr.Kind, entityType.Name),
                    Defaulted(attr.ApiVersion, "v1"),
                    attr.Group,
                    attr.PluralName),
                scope switch
                {
                    null => Enum.GetName(typeof(EntityScope), EntityScope.Namespaced) ?? "namespaced",
                    _ => Enum.GetName(typeof(EntityScope), scope.Scope) ?? "namespaced",
                }),
        };

    private static string Defaulted(string value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value;
}
