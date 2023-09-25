module KubeOps.Transpiler.Entities

open System
open System.Reflection
open KubeOps.Abstractions.Entities
open KubeOps.Abstractions.Entities.Attributes
open k8s.Models

/// <summary>
/// Converts the given type to an <see cref="EntityMetadata"/> object.
/// </summary>
/// <param name="entityType">The type to convert.</param>
/// <returns>The <see cref="EntityMetadata"/> object representing the type.</returns>
/// <exception cref="ArgumentException">Thrown if the given type is not a valid Kubernetes entity.</exception>
let ToEntityMetadata (entityType: Type) =
    match
        (entityType.GetCustomAttribute<KubernetesEntityAttribute>(),
         entityType.GetCustomAttribute<EntityScopeAttribute>())
    with
    | null, _ -> raise <| ArgumentException "The given type is not a valid Kubernetes entity."
    | attr, scope ->
        (EntityMetadata(
            attr.Kind,
            attr.ApiVersion,
            (if String.IsNullOrWhiteSpace(attr.Group) then
                 null
             else
                 attr.Group),
            (if String.IsNullOrWhiteSpace(attr.PluralName) then
                 null
             else
                 attr.PluralName)
         ),
         match scope with
         | null -> EntityScope.Namespaced.ToString()
         | _ -> scope.Scope.ToString())
