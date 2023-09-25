module KubeOps.Transpiler.Crds

open System
open System.Collections
open System.Collections.Generic
open System.Linq
open System.Reflection
open System.Text.Json.Serialization
open System.Text.RegularExpressions
open KubeOps.Abstractions.Entities
open KubeOps.Abstractions.Entities.Attributes
open k8s
open k8s.Models
open Namotion.Reflection

let private ignoredToplevelProperties = [ "metadata"; "apiversion"; "kind" ]

type private KubernetesVersionComparer() =
    let regex =
        Regex(@"^v(?<major>[0-9]+)((?<stream>alpha|beta)(?<minor>[0-9]+))?$", RegexOptions.Compiled)

    let extractVersion (m: Match) =
        let major = int m.Groups["major"].Value
        let streamString = m.Groups["stream"].Value

        let stream =
            match streamString with
            | "alpha" -> 1
            | "beta" -> 2
            | _ -> 3

        let minor =
            match Int32.TryParse m.Groups["minor"].Value with
            | true, value -> value
            | _ -> 0

        Version(major, stream, minor)

    interface IComparer<string> with
        member this.Compare(x, y) =
            if x = null || y = null then
                StringComparer.CurrentCulture.Compare(x, y)
            else
                let mX = regex.Match(x)
                let mY = regex.Match(y)

                if not mX.Success || not mY.Success then
                    StringComparer.CurrentCulture.Compare(x, y)
                else
                    let x = extractVersion mX
                    let y = extractVersion mY
                    x.CompareTo(y)

let private propertyName (prop: PropertyInfo) =
    let name =
        match prop.GetCustomAttribute<JsonPropertyNameAttribute>() with
        | null -> prop.Name
        | attr -> attr.Name

    $"{Char.ToLowerInvariant name[0]}{name[1..]}"

module private AttributeMapping =
    let private createMapper<'T when 'T :> Attribute and 'T: null>
        fn
        (prop: PropertyInfo)
        (props: V1JSONSchemaProps ref)
        =
        match prop.GetCustomAttribute<'T>() with
        | null -> ()
        | attr -> fn attr props

    let private extDoc =
        createMapper<ExternalDocsAttribute> (fun attr props ->
            props.Value.ExternalDocs <- V1ExternalDocumentation(attr.Description, attr.Url)
            ())

    let private itemsAttr =
        createMapper<ItemsAttribute> (fun attr props ->
            if attr.MinItems.HasValue then
                props.Value.MinItems <- attr.MinItems.Value

            if attr.MaxItems.HasValue then
                props.Value.MaxItems <- attr.MaxItems.Value

            ())

    let private lengthAttr =
        createMapper<LengthAttribute> (fun attr props ->
            if attr.MinLength.HasValue then
                props.Value.MinLength <- attr.MinLength.Value

            if attr.MaxLength.HasValue then
                props.Value.MaxLength <- attr.MaxLength.Value

            ())

    let private multipleOfAttr =
        createMapper<MultipleOfAttribute> (fun attr props ->
            props.Value.MultipleOf <- attr.Value
            ())

    let private patternAttr =
        createMapper<PatternAttribute> (fun attr props ->
            props.Value.Pattern <- attr.RegexPattern
            ())

    let private rangeMaxAttr =
        createMapper<RangeMaximumAttribute> (fun attr props ->
            props.Value.Maximum <- attr.Maximum
            props.Value.ExclusiveMaximum <- attr.ExclusiveMaximum
            ())

    let private rangeMinAttr =
        createMapper<RangeMinimumAttribute> (fun attr props ->
            props.Value.Minimum <- attr.Minimum
            props.Value.ExclusiveMinimum <- attr.ExclusiveMinimum
            ())

    let private preserveUnknownAttr =
        createMapper<PreserveUnknownFieldsAttribute> (fun _ props ->
            props.Value.XKubernetesPreserveUnknownFields <- true
            ())

    let private embeddedAttr =
        createMapper<EmbeddedResourceAttribute> (fun _ props ->
            props.Value.XKubernetesPreserveUnknownFields <- true
            props.Value.XKubernetesEmbeddedResource <- true
            props.Value.Properties <- null
            props.Value.Type <- "object"
            ())

    let private mappers =
        [ extDoc
          itemsAttr
          lengthAttr
          multipleOfAttr
          patternAttr
          rangeMaxAttr
          rangeMinAttr
          preserveUnknownAttr
          embeddedAttr ]

    let map (prop: PropertyInfo) (props: V1JSONSchemaProps ref) =
        mappers |> List.iter (fun m -> m prop props)
        ()

module private TypeMapping =
    type PropType =
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

    let private v1ObjectMeta (t: Type) _ =
        if t = typeof<V1ObjectMeta> then
            Some(V1JSONSchemaProps(Type = PropType.Object.value))
        else
            None

    let private isArray (t: Type) map =
        if t.IsArray then
            Some(V1JSONSchemaProps(Type = PropType.Array.value, Items = (map <| t.GetElementType())))
        else
            None

    let private isResourceQuantityDictionary (t: Type) _ =
        if
            t.IsGenericType
            && t.GetGenericTypeDefinition() = typeof<IDictionary<_, _>>
            && t.GenericTypeArguments.Contains(typeof<ResourceQuantity>)
        then
            Some(V1JSONSchemaProps(Type = PropType.Object.value, XKubernetesPreserveUnknownFields = true))
        else
            None

    let private isDictionary (t: Type) map =
        if t.IsGenericType && t.GetGenericTypeDefinition() = typeof<IDictionary<_, _>> then
            Some(
                V1JSONSchemaProps(
                    Type = PropType.Object.value,
                    AdditionalProperties = (map <| t.GenericTypeArguments[1])
                )
            )
        else
            None

    let private isEnumerableKeyValuePair (t: Type) map =
        if
            t.IsGenericType
            && t.GetGenericTypeDefinition() = typeof<IEnumerable<_>>
            && t.GenericTypeArguments.Length = 1
            && t.GenericTypeArguments.First().IsGenericType
            && t.GenericTypeArguments.First().GetGenericTypeDefinition() = typeof<KeyValuePair<_, _>>
        then
            Some(
                V1JSONSchemaProps(
                    Type = PropType.Object.value,
                    AdditionalProperties = (map <| t.GenericTypeArguments.First().GenericTypeArguments[1])
                )
            )
        else
            None

    let private isArbitraryDictionary (t: Type) _ =
        if
            typeof<IDictionary>.IsAssignableFrom t
            || (t.IsGenericType
                && t.GetGenericTypeDefinition() = typeof<IEnumerable<_>>
                && t.GenericTypeArguments.Any()
                && t.GenericTypeArguments.First().IsGenericType
                && t.GenericTypeArguments.First().GetGenericTypeDefinition() = typeof<KeyValuePair<_, _>>)
        then
            Some(V1JSONSchemaProps(Type = PropType.Object.value, XKubernetesPreserveUnknownFields = true))
        else
            None

    let private isArbitraryEnumerableKeyValuePair (t: Type) _ =
        if
            t.IsGenericType
            && (typeof<IEnumerable>.IsAssignableFrom <| t.GetGenericTypeDefinition())
            || (t.IsGenericType
                && t.GetGenericTypeDefinition() = typeof<IEnumerable<_>>
                && t.GenericTypeArguments.Any()
                && t.GenericTypeArguments.First().IsGenericType
                && t.GenericTypeArguments.First().GetGenericTypeDefinition() = typeof<KeyValuePair<_, _>>)
        then
            Some(V1JSONSchemaProps(Type = PropType.Array.value, XKubernetesPreserveUnknownFields = true))
        else
            None

    let private isArbitraryEnumerable (t: Type) map =
        if
            t.IsGenericType
            && (typeof<IEnumerable>.IsAssignableFrom <| t.GetGenericTypeDefinition())
        then
            Some(V1JSONSchemaProps(Type = PropType.Array.value, Items = (map <| t.GenericTypeArguments[0])))
        else
            None

    let private hasEnumerableInterface (t: Type) map =
        if
            t.GetInterfaces()
            |> Array.filter (fun i -> i.IsGenericType && i.GetGenericTypeDefinition() = typeof<IEnumerable<_>>)
            |> Array.length > 0
        then
            Some(
                V1JSONSchemaProps(
                    Type = PropType.Array.value,
                    Items =
                        (map
                         <| (t.GetInterfaces()
                             |> Array.filter (fun i ->
                                 i.IsGenericType && i.GetGenericTypeDefinition() = typeof<IEnumerable<_>>)
                             |> Array.head))
                )
            )
        else
            None

    let private isIntOrString (t: Type) _ =
        if t = typeof<IntstrIntOrString> then
            Some(V1JSONSchemaProps(XKubernetesIntOrString = true))
        else
            None

    let private isKubernetesObject (t: Type) _ =
        if
            typeof<IKubernetesObject>.IsAssignableFrom t
            && not t.IsAbstract
            && not t.IsInterface
            && typeof<IKubernetesObject>.Assembly <> t.Assembly
        then
            Some(
                V1JSONSchemaProps(
                    Type = PropType.Object.value,
                    Properties = null,
                    XKubernetesPreserveUnknownFields = true,
                    XKubernetesEmbeddedResource = true
                )
            )
        else
            None

    let private isInt (t: Type) _ =
        if t = typeof<Int32> || Nullable.GetUnderlyingType(t) = typeof<int> then
            Some(V1JSONSchemaProps(Type = PropType.Integer.value, Format = Format.Int32.value))
        else
            None

    let private isInt64 (t: Type) _ =
        if t = typeof<Int64> || Nullable.GetUnderlyingType(t) = typeof<int64> then
            Some(V1JSONSchemaProps(Type = PropType.Integer.value, Format = Format.Int64.value))
        else
            None

    let private isFloat (t: Type) _ =
        if
            t = typeof<float>
            || Nullable.GetUnderlyingType(t) = typeof<float>
            || t = typeof<Single>
            || Nullable.GetUnderlyingType(t) = typeof<Single>
        then
            Some(V1JSONSchemaProps(Type = PropType.Number.value, Format = Format.Float.value))
        else
            None

    let private isDouble (t: Type) _ =
        if
            t = typeof<double>
            || Nullable.GetUnderlyingType(t) = typeof<double>
            || t = typeof<Double>
            || Nullable.GetUnderlyingType(t) = typeof<Double>
        then
            Some(V1JSONSchemaProps(Type = PropType.Number.value, Format = Format.Double.value))
        else
            None

    let private isString (t: Type) _ =
        if t = typeof<string> || Nullable.GetUnderlyingType(t) = typeof<string> then
            Some(V1JSONSchemaProps(Type = PropType.String.value))
        else
            None

    let private isBool (t: Type) _ =
        if t = typeof<bool> || Nullable.GetUnderlyingType(t) = typeof<bool> then
            Some(V1JSONSchemaProps(Type = PropType.Boolean.value))
        else
            None

    let private isDateTime (t: Type) _ =
        if t = typeof<DateTime> || Nullable.GetUnderlyingType(t) = typeof<DateTime> then
            Some(V1JSONSchemaProps(Type = PropType.String.value, Format = Format.DateTime.value))
        else
            None

    let private isEnum (t: Type) _ =
        if t.IsEnum then
            Some(V1JSONSchemaProps(Type = PropType.String.value, EnumProperty = t.GetEnumNames().Cast<obj>().ToList()))
        else
            None

    let private isNullableEnum (t: Type) _ =
        if Nullable.GetUnderlyingType(t) <> null && Nullable.GetUnderlyingType(t).IsEnum then
            Some(
                V1JSONSchemaProps(
                    Type = PropType.String.value,
                    EnumProperty = Nullable.GetUnderlyingType(t).GetEnumNames().Cast<obj>().ToList()
                )
            )
        else
            None

    let private isNotSimpleType (t: Type) map =
        if not <| isSimpleType t then
            Some(
                V1JSONSchemaProps(
                    Type = PropType.Object.value,
                    Properties =
                        (t.GetProperties()
                         |> Array.filter (fun p -> p.GetCustomAttribute<IgnoreAttribute>() = null)
                         |> Array.map (fun p ->
                             let schema = map p.PropertyType
                             AttributeMapping.map p (ref schema)
                             (propertyName p, schema))
                         |> dict),
                    Required =
                        match
                            t.GetProperties()
                            |> Array.filter (fun p -> p.GetCustomAttribute<RequiredAttribute>() <> null)
                            |> Array.filter (fun p -> p.GetCustomAttribute<IgnoreAttribute>() = null)
                            |> Array.map propertyName
                        with
                        | [||] -> null
                        | props -> props.ToList()
                )
            )
        else
            None

    let private mappers =
        [ v1ObjectMeta
          isArray
          isResourceQuantityDictionary
          isDictionary
          isEnumerableKeyValuePair
          isArbitraryDictionary
          isArbitraryEnumerableKeyValuePair
          isArbitraryEnumerable
          hasEnumerableInterface
          isIntOrString
          isKubernetesObject
          isInt
          isInt64
          isFloat
          isDouble
          isString
          isBool
          isDateTime
          isEnum
          isNullableEnum
          isNotSimpleType ]

    let rec map (t: Type) =
        let rec mapping (t: Type) mappers =
            match mappers with
            | [] -> failwithf $"Unsupported type: %s{t.FullName}"
            | m :: mappers ->
                match m t map with
                | Some props -> props
                | None -> mapping t mappers

        mapping t mappers

let private map (prop: PropertyInfo) =
    let description =
        match prop.GetCustomAttributes<DescriptionAttribute>(true) |> Seq.tryHead with
        | Some attr -> attr.Description
        | None -> null

    let mutable props = TypeMapping.map prop.PropertyType

    let ctx = prop.ToContextualProperty()

    if props.Description = null then
        props.Description <- description

    if
        props.Description = null
        && not <| String.IsNullOrWhiteSpace(ctx.GetXmlDocsSummary())
    then
        props.Description <- ctx.GetXmlDocsSummary()

    if ctx.Nullability = Nullability.Nullable then
        props.Nullable <- true

    AttributeMapping.map prop (ref props)

    props

let private mapPrinterCols (t: Type) =
    let mutable res = []

    let mutable props =
        (List.ofArray <| t.GetProperties()) |> List.map (fun p -> (p, String.Empty))

    while not props.IsEmpty do
        match props with
        | [] -> failwith "Props should not be empty."
        | (prop, path) :: rest ->
            props <- rest

            if prop.PropertyType.IsClass then
                props <-
                    props
                    @ (List.ofArray <| prop.PropertyType.GetProperties()
                       |> List.map (fun p -> (p, $"{path}.{propertyName prop}")))

            match prop.GetCustomAttribute<AdditionalPrinterColumnAttribute>() with
            | null -> ()
            | attr ->
                let p = map prop

                let name =
                    match attr.Name with
                    | null -> propertyName prop
                    | name -> name

                res <-
                    res
                    @ [ V1CustomResourceColumnDefinition(
                            Name = name,
                            JsonPath = $"{path}.{propertyName prop}",
                            Type = p.Type,
                            Description = p.Description,
                            Format = p.Format,
                            Priority =
                                match attr.Priority with
                                | PrinterColumnPriority.StandardView -> 0
                                | _ -> 1
                        ) ]

    res
    @ (List.ofSeq
       <| t.GetCustomAttributes<GenericAdditionalPrinterColumnAttribute>(true)
       |> List.map (fun a ->
           V1CustomResourceColumnDefinition(
               Name = a.Name,
               JsonPath = a.JsonPath,
               Type = a.Type,
               Description = a.Description,
               Format = a.Format,
               Priority =
                   match a.Priority with
                   | PrinterColumnPriority.StandardView -> 0
                   | _ -> 1
           )))

/// <summary>
/// Transpiles the given Kubernetes entity type to a <see cref="V1CustomResourceDefinition"/> object.
/// </summary>
/// <param name="entityType">The Kubernetes entity type to transpile.</param>
/// <returns>A <see cref="V1CustomResourceDefinition"/> object representing the transpiled entity type.</returns>
/// <exception cref="ArgumentException">Thrown if the given type is not a valid Kubernetes entity.</exception>
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
                Type = TypeMapping.PropType.Object.value,
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

    version.AdditionalPrinterColumns <-
        match mapPrinterCols entityType with
        | [] -> null
        | cols -> cols.ToList()

    crd.Spec.Versions <- [| version |]
    crd.Validate()

    crd

/// <summary>
/// Transpiles the given sequence of Kubernetes entity types to a
/// sequence of <see cref="V1CustomResourceDefinition"/> objects.
/// The definitions are grouped by version and one stored version is defined.
/// The transpiler fails when multiple stored versions are defined.
/// </summary>
/// <param name="entities">The sequence of Kubernetes entity types to transpile.</param>
/// <returns>A sequence of <see cref="V1CustomResourceDefinition"/> objects representing the transpiled entity types.</returns>
let TranspileByVersion (entities: Type seq) =
    entities
    |> Seq.filter (fun t -> t.GetCustomAttribute<IgnoreAttribute>() = null)
    |> Seq.filter (fun t -> t.GetCustomAttribute<KubernetesEntityAttribute>() <> null)
    |> Seq.filter (fun t -> t.Assembly <> typeof<KubernetesEntityAttribute>.Assembly)
    |> Seq.map (fun t -> (Transpile t, t.GetCustomAttributes<StorageVersionAttribute>().Any()))
    |> Seq.groupBy (fun (crd, _) -> crd.Metadata.Name)
    |> Seq.map (fun (key, values) ->
        if values.Count(snd) > 1 then
            failwithf $"Multiple storage versions for %s{key}"

        let mutable crd = values |> Seq.head |> fst

        let versions =
            values
            |> Seq.collect (fun (crd, stored) ->
                crd.Spec.Versions
                |> Seq.map (fun v ->
                    v.Served <- true
                    v.Storage <- stored
                    v))

        crd.Spec.Versions <-
            versions
                .OrderByDescending(fun v -> v.Name, KubernetesVersionComparer())
                .ToList()

        if crd.Spec.Versions.Count = 1 || values.Count(snd) = 0 then
            crd.Spec.Versions.First().Storage <- true

        crd)
