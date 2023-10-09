using System.Reflection;

using k8s.KubeConfigModels;
using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace KubeOps.Cli.Transpilation;

internal static class Entities
{
    public static (EntityMetadata Metadata, string Scope) ToEntityMetadata(MetadataLoadContext context, Type entityType)
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
                        null => Enum.GetName(EntityScope.Namespaced) ?? "namespaced",
                        _ => Enum.GetName(
                            scope.GetCustomAttributeCtorArg<EntityScope>(context, 0)) ?? "namespaced",
                    }),
            };

    private static string Defaulted(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value;
}
