module KubeOps.Transpiler.Entities

open System
open System.Reflection
open KubeOps.Abstractions.Entities
open KubeOps.Abstractions.Entities.Attributes
open k8s.Models

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
