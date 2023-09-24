module KubeOps.Transpiler.Crds

open System
open System.Collections
open System.Collections.Generic
open System.Linq
open System.Reflection
open System.Text.Json.Serialization
open KubeOps.Abstractions.Entities.Attributes
open k8s
open k8s.Models
open Namotion.Reflection

type private PropType =
    | Object
    | Array
    | String
    | Integer
    | Number
    | Boolean

    member self.value =
        match self with
        | Object -> "object"
        | Array -> "array"
        | String -> "string"
        | Integer -> "integer"
        | Number -> "number"
        | Boolean -> "boolean"

type private Format =
    | Int32
    | Int64
    | Float
    | Double
    | DateTime

    member self.value =
        match self with
        | Int32 -> "int32"
        | Int64 -> "int64"
        | Float -> "float"
        | Double -> "double"
        | DateTime -> "date-time"

let private ignoredToplevelProperties = [ "metadata"; "apiversion"; "kind" ]

let private propertyName (prop: PropertyInfo) =
    let name =
        match prop.GetCustomAttribute<JsonPropertyNameAttribute>() with
        | null -> prop.Name
        | attr -> attr.Name

    $"{Char.ToLowerInvariant name[0]}{name[1..]}"

let rec private isSimpleType (t: Type) =
    t.IsPrimitive
    || t = typeof<string>
    || t = typeof<decimal>
    || t = typeof<DateTime>
    || t = typeof<DateTimeOffset>
    || t = typeof<TimeSpan>
    || t = typeof<Guid>
    || t.IsEnum
    || Convert.GetTypeCode(t) <> TypeCode.Object
    || (t.IsGenericType
        && t.GetGenericTypeDefinition() = typeof<Nullable<_>>
        && isSimpleType (t.GetGenericArguments() |> Seq.head))

let rec private mapType (t: Type) =
    let description =
        match t.GetCustomAttributes<DescriptionAttribute>(true) |> Seq.tryHead with
        | Some attr -> attr.Description
        | None -> null

    let mutable props =
        match (t, isSimpleType t) with
        | t, _ when t = typeof<V1ObjectMeta> -> V1JSONSchemaProps(Type = PropType.Object.value)
        | t, _ when t.IsArray -> V1JSONSchemaProps(Type = PropType.Array.value, Items = (mapType <| t.GetElementType()))
        | t, false when t.IsGenericType && t.GetGenericTypeDefinition() = typeof<IDictionary<_, _>> ->
            V1JSONSchemaProps(
                Type = PropType.Object.value,
                AdditionalProperties = (mapType <| t.GenericTypeArguments.[1])
            )
        | t, false when
            t.IsGenericType
            && t.GetGenericTypeDefinition() = typeof<IEnumerable<_>>
            && t.GenericTypeArguments.Length = 1
            && t.GenericTypeArguments.First().IsGenericType
            && t.GenericTypeArguments.First().GetGenericTypeDefinition() = typeof<KeyValuePair<_, _>>
            ->
            V1JSONSchemaProps(
                Type = PropType.Object.value,
                AdditionalProperties = (mapType <| t.GenericTypeArguments.First().GenericTypeArguments.[1])
            )
        | t, false when
            typeof<IDictionary>.IsAssignableFrom t
            || (t.IsGenericType
                && t.GetGenericTypeDefinition() = typeof<IEnumerable<_>>
                && t.GenericTypeArguments.Any()
                && t.GenericTypeArguments.First().IsGenericType
                && t.GenericTypeArguments.First().GetGenericTypeDefinition() = typeof<KeyValuePair<_, _>>)
            ->
            V1JSONSchemaProps(Type = PropType.Object.value, XKubernetesPreserveUnknownFields = true)
        | t, false when
            t.IsGenericType
            && (typeof<IEnumerable>.IsAssignableFrom <| t.GetGenericTypeDefinition())
            || (t.IsGenericType
                && t.GetGenericTypeDefinition() = typeof<IEnumerable<_>>
                && t.GenericTypeArguments.Any()
                && t.GenericTypeArguments.First().IsGenericType
                && t.GenericTypeArguments.First().GetGenericTypeDefinition() = typeof<KeyValuePair<_, _>>)
            ->
            V1JSONSchemaProps(Type = PropType.Array.value, XKubernetesPreserveUnknownFields = true)
        | t, false when
            t.IsGenericType
            && (typeof<IEnumerable<_>>.IsAssignableFrom <| t.GetGenericTypeDefinition())
            ->
            V1JSONSchemaProps(Type = PropType.Array.value, Items = (mapType <| t.GenericTypeArguments[0]))
        | t, false when
            t.GetInterfaces()
            |> Array.filter (fun i -> i.IsGenericType && i.GetGenericTypeDefinition() = typeof<IEnumerable<_>>)
            |> Array.length > 0
            ->
            V1JSONSchemaProps(
                Type = PropType.Array.value,
                Items =
                    (mapType
                     <| (t.GetInterfaces()
                         |> Array.filter (fun i ->
                             i.IsGenericType && i.GetGenericTypeDefinition() = typeof<IEnumerable<_>>)
                         |> Array.head))
            )
        | t, _ when t = typeof<IntstrIntOrString> -> V1JSONSchemaProps(XKubernetesIntOrString = true)
        | t, _ when
            typeof<IKubernetesObject>.IsAssignableFrom t
            && not t.IsAbstract
            && not t.IsInterface
            && typeof<IKubernetesObject>.Assembly <> t.Assembly
            ->
            V1JSONSchemaProps(
                Type = PropType.Object.value,
                Properties = null,
                XKubernetesPreserveUnknownFields = true,
                XKubernetesEmbeddedResource = true
            )
        | t, _ when t = typeof<int> || Nullable.GetUnderlyingType(t) = typeof<int> ->
            V1JSONSchemaProps(Type = PropType.Integer.value, Format = Format.Int32.value)
        | t, _ when t = typeof<int64> || Nullable.GetUnderlyingType(t) = typeof<int64> ->
            V1JSONSchemaProps(Type = PropType.Integer.value, Format = Format.Int64.value)
        | t, _ when t = typeof<float> || Nullable.GetUnderlyingType(t) = typeof<float> ->
            V1JSONSchemaProps(Type = PropType.Number.value, Format = Format.Float.value)
        | t, _ when t = typeof<double> || Nullable.GetUnderlyingType(t) = typeof<double> ->
            V1JSONSchemaProps(Type = PropType.Number.value, Format = Format.Double.value)
        | t, _ when t = typeof<string> || Nullable.GetUnderlyingType(t) = typeof<string> ->
            V1JSONSchemaProps(Type = PropType.String.value)
        | t, _ when t = typeof<bool> || Nullable.GetUnderlyingType(t) = typeof<bool> ->
            V1JSONSchemaProps(Type = PropType.Boolean.value)
        | t, _ when t = typeof<DateTime> || Nullable.GetUnderlyingType(t) = typeof<DateTime> ->
            V1JSONSchemaProps(Type = PropType.String.value, Format = Format.DateTime.value)
        | t, _ when t.IsEnum ->
            V1JSONSchemaProps(Type = PropType.String.value, EnumProperty = t.GetEnumNames().Cast<obj>().ToList())
        | t, _ when Nullable.GetUnderlyingType(t) <> null && Nullable.GetUnderlyingType(t).IsEnum ->
            V1JSONSchemaProps(
                Type = PropType.String.value,
                EnumProperty = Nullable.GetUnderlyingType(t).GetEnumNames().Cast<obj>().ToList()
            )
        | t, false ->
            V1JSONSchemaProps(
                Type = PropType.Object.value,
                Properties =
                    (t.GetProperties()
                     |> Array.filter (fun p -> p.GetCustomAttribute<IgnorePropertyAttribute>() = null)
                     |> Array.map (fun p -> (propertyName p, map p))
                     |> dict),
                Required =
                    match
                        t.GetProperties()
                        |> Array.filter (fun p -> p.GetCustomAttribute<RequiredAttribute>() <> null)
                        |> Array.filter (fun p -> p.GetCustomAttribute<IgnorePropertyAttribute>() = null)
                        |> Array.map propertyName
                    with
                    | [||] -> null
                    | props -> props.ToList()
            )
        | _ -> failwithf $"Unsupported type: %s{t.Name}"

    props.Description <- description

    props

and private map (prop: PropertyInfo) =
    let description =
        match prop.GetCustomAttributes<DescriptionAttribute>(true) |> Seq.tryHead with
        | Some attr -> attr.Description
        | None -> null

    let mutable props = mapType prop.PropertyType

    if props.Description = null then
        props.Description <- description

    let ctx = prop.ToContextualProperty()
    if ctx.Nullability = Nullability.Nullable then
        props.Nullable <- true
    // TODO: prop stuff.
    props

let Transpile (entityType: Type) =
    let entityMetadata, scope = Entities.ToEntityMetadata entityType

    let mutable crd =
        V1CustomResourceDefinition(V1CustomResourceDefinitionSpec()).Initialize()

    crd.Metadata.Name <- $"{entityMetadata.PluralName}.{entityMetadata.Group}"
    crd.Spec.Group <- entityMetadata.Group

    crd.Spec.Names <-
        V1CustomResourceDefinitionNames(
            Kind = entityMetadata.Kind,
            ListKind = entityMetadata.ListKind,
            Singular = entityMetadata.SingularName,
            Plural = entityMetadata.PluralName,
            ShortNames =
                match
                    List.ofSeq (entityType.GetCustomAttributes<KubernetesEntityShortNamesAttribute>(true))
                    |> List.collect (fun a -> List.ofArray a.ShortNames)
                with
                | [] -> null
                | shortNames -> shortNames.ToList()
        )

    crd.Spec.Scope <- scope

    let mutable version =
        V1CustomResourceDefinitionVersion(entityMetadata.Version, true, true)

    if
        (entityType.GetProperty("Status") <> null
         || entityType.GetProperty("status") <> null)
    then
        version.Subresources <- V1CustomResourceSubresources(null, obj ())

    version.Schema <-
        V1CustomResourceValidation(
            V1JSONSchemaProps(
                Type = PropType.Object.value,
                Description =
                    (match entityType.GetCustomAttributes<DescriptionAttribute>(true) |> Seq.tryHead with
                     | Some attr -> attr.Description
                     | None -> null),
                Properties =
                    (entityType.GetProperties()
                     |> Array.filter (fun p -> not <| (ignoredToplevelProperties.Contains <| p.Name.ToLowerInvariant()))
                     |> Array.map (fun p -> (propertyName p, map p))
                     |> dict)
            )
        )
    // TODO: printer cols
    crd.Spec.Versions <- [| version |]

    crd
