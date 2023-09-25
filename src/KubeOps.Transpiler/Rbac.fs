module KubeOps.Transpiler.Rbac

open System
open System.Linq
open KubeOps.Abstractions.Rbac
open k8s.Models

let private convertToStrings verbs =
    let values =
        match verbs with
        | RbacVerb.None -> Seq.empty
        | _ when verbs.HasFlag(RbacVerb.All) -> seq { "*" }
        | _ ->
#if NETSTANDARD
            Enum.GetValues(typeof<RbacVerb>).Cast<RbacVerb>()
#else
            Enum.GetValues<RbacVerb>()
#endif
            |> Seq.filter (fun x -> x <> RbacVerb.None && x <> RbacVerb.All)
            |> Seq.filter verbs.HasFlag
            |> Seq.map (fun x -> x.ToString().ToLower())

    values.ToList()

let private transpileGenericAttribute (a: GenericRbacAttribute) =
    V1PolicyRule(
        ApiGroups = a.Groups,
        Resources = a.Resources,
        NonResourceURLs = a.Urls,
        Verbs = convertToStrings a.Verbs
    )

let private transpileEntityAttributes (attributes: EntityRbacAttribute seq) =
    attributes
    |> Seq.collect (fun a -> a.Entities |> Seq.map (fun e -> (e, a.Verbs)))
    |> Seq.groupBy fst
    |> Seq.map (fun (e, g) ->
        (Entities.ToEntityMetadata e, g |> Seq.map snd |> Seq.fold (fun acc verb -> acc ||| verb) RbacVerb.None))
    |> Seq.groupBy snd
    |> Seq.map (fun (verbs, group) -> (verbs, group |> Seq.map fst |> Seq.map fst))
    |> Seq.map (fun (verbs, crds) ->
        V1PolicyRule(
            ApiGroups = (crds |> Seq.map (fun x -> x.Group) |> Seq.distinct).ToList(),
            Resources = (crds |> Seq.map (fun x -> x.Plural) |> Seq.distinct).ToList(),
            Verbs = convertToStrings verbs
        ))

let private transpileEntityStatusAttributes (attributes: EntityRbacAttribute seq) =
    attributes
    |> Seq.collect (fun a -> a.Entities |> Seq.map (fun e -> (e, a.Verbs)))
    |> Seq.filter (fun (e, _) -> e.GetProperty("Status") <> null)
    |> Seq.groupBy fst
    |> Seq.map fst
    |> Seq.map Entities.ToEntityMetadata
    |> Seq.map fst
    |> Seq.map (fun e ->
        V1PolicyRule(
            ApiGroups = [ e.Group ].ToList(),
            Resources = [ $"{e.Plural}/status" ].ToList(),
            Verbs = convertToStrings (RbacVerb.Get ||| RbacVerb.Patch ||| RbacVerb.Update)
        ))

let Transpile (attributes: RbacAttribute seq) =
    let generic =
        attributes
        |> Seq.filter (fun x -> x :? GenericRbacAttribute)
        |> Seq.cast<GenericRbacAttribute>
        |> Seq.map transpileGenericAttribute

    let entityAttrs =
        attributes
        |> Seq.filter (fun x -> x :? EntityRbacAttribute)
        |> Seq.cast<EntityRbacAttribute>

    let entity = transpileEntityAttributes entityAttrs
    let entityStatus = transpileEntityStatusAttributes entityAttrs

    Seq.concat [ generic; entity; entityStatus ]
