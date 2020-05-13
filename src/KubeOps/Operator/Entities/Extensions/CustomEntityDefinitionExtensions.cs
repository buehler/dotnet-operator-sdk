using System;
using System.Reflection;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Entities.Extensions
{
    internal static class CustomEntityDefinitionExtensions
    {
        internal static CustomEntityDefinition CreateResourceDefinition(
            this IKubernetesObject<V1ObjectMeta> kubernetesEntity) =>
            CreateResourceDefinition(kubernetesEntity.GetType());

        internal static CustomEntityDefinition CreateResourceDefinition<TEntity>()
            where TEntity : IKubernetesObject<V1ObjectMeta> =>
            CreateResourceDefinition(typeof(TEntity));

        internal static CustomEntityDefinition CreateResourceDefinition(Type resourceType)
        {
            var attribute = resourceType.GetCustomAttribute<KubernetesEntityAttribute>();
            if (attribute == null)
            {
                throw new ArgumentException($"The Type {resourceType} does not have the kubernetes entity attribute.");
            }

            var scopeAttribute = resourceType.GetCustomAttribute<EntityScopeAttribute>();
            var kind = string.IsNullOrWhiteSpace(attribute.Kind) ? resourceType.Name : attribute.Kind;

            return new CustomEntityDefinition(
                kind,
                $"{kind}List",
                attribute.Group,
                attribute.ApiVersion,
                kind.ToLower(),
                string.IsNullOrWhiteSpace(attribute.PluralName) ? $"{kind.ToLower()}s" : attribute.PluralName,
                scopeAttribute?.Scope ?? default);
        }
    }
}
