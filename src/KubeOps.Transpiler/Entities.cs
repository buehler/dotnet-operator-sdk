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
        => (entityType.GetCustomAttributeData<KubernetesEntityAttribute>(),
                entityType.GetCustomAttributeData<EntityScopeAttribute>()) switch
            {
                (null, _) => throw new ArgumentException("The given type is not a valid Kubernetes entity."),
                ({ } attr, var scope) => (new(
                        Defaulted(
                            attr.GetCustomAttributeNamedArg<string>(nameof(KubernetesEntityAttribute.Kind)),
                            entityType.Name),
                        Defaulted(
                            attr.GetCustomAttributeNamedArg<string>(nameof(KubernetesEntityAttribute.ApiVersion)),
                            "v1"),
                        attr.GetCustomAttributeNamedArg<string>(nameof(KubernetesEntityAttribute.Group)),
                        attr.GetCustomAttributeNamedArg<string>(nameof(KubernetesEntityAttribute.PluralName))),
                    scope switch
                    {
                        null => Enum.GetName(typeof(EntityScope), EntityScope.Namespaced) ?? "namespaced",
                        _ => Enum.GetName(
                                 typeof(EntityScope),
                                 attr.GetCustomAttributeNamedArg<EntityScope>(nameof(EntityScopeAttribute.Scope))) ??
                             "namespaced",
                    }),
            };

    private static string Defaulted(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value;
}
