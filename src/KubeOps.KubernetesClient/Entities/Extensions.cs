using System.Reflection;
using k8s;
using k8s.Models;

namespace KubeOps.KubernetesClient.Entities;

public static class Extensions
{
    /// <summary>
    /// Create a custom entity definition.
    /// </summary>
    /// <param name="resource">The resource that is used as the type.</param>
    /// <returns>A <see cref="EntityDefinition"/>.</returns>
    public static EntityDefinition ToEntityDefinition(
        this IKubernetesObject<V1ObjectMeta> resource) =>
        ToEntityDefinition(resource.GetType());

    /// <summary>
    /// Create a custom entity definition.
    /// </summary>
    /// <typeparam name="TResource">The concrete type of the resource.</typeparam>
    /// <returns>A <see cref="EntityDefinition"/>.</returns>
    public static EntityDefinition ToEntityDefinition<TResource>()
        where TResource : IKubernetesObject<V1ObjectMeta> =>
        ToEntityDefinition(typeof(TResource));

    /// <summary>
    /// Create a custom entity definition.
    /// </summary>
    /// <param name="resourceType">A type to construct the definition from.</param>
    /// <exception cref="ArgumentException">
    /// When the type of the resource does not contain a <see cref="KubernetesEntityAttribute"/>.
    /// </exception>
    /// <returns>A <see cref="EntityDefinition"/>.</returns>
    public static EntityDefinition ToEntityDefinition(this Type resourceType)
    {
        var attribute = resourceType.GetCustomAttribute<KubernetesEntityAttribute>();
        if (attribute == null)
        {
            throw new ArgumentException($"The Type {resourceType} does not have the kubernetes entity attribute.");
        }

        var scopeAttribute = resourceType.GetCustomAttribute<EntityScopeAttribute>();
        var kind = string.IsNullOrWhiteSpace(attribute.Kind) ? resourceType.Name : attribute.Kind;

        return new EntityDefinition(
            kind,
            $"{kind}List",
            attribute.Group,
            attribute.ApiVersion,
            kind.ToLower(),
            string.IsNullOrWhiteSpace(attribute.PluralName) ? $"{kind.ToLower()}s" : attribute.PluralName,
            scopeAttribute?.Scope ?? default);
    }
}
