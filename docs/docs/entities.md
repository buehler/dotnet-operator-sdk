# Custom Entities

The words `entity` and `resource` are kind of interchangeable. It strongly
depends on the context. The resource is the type of an object in kubernetes
which is defined by the default api or a CRD. While an entity is a class
in C# of such a resource. (CRD means "custom resource definition").

To write your own kubernetes entities, use the interfaces
provided by `k8s` or use the <xref:KubeOps.Operator.Entities.CustomKubernetesEntity>.

There are two overloads (<xref:KubeOps.Operator.Entities.CustomKubernetesEntity`1> and <xref:KubeOps.Operator.Entities.CustomKubernetesEntity`2>) that provide
generics for the `Spec` and `Status` resource values.

A "normal" entity does not provide any real value (i.e. most of the time).
Normally you need some kind of `Spec` to have data in your entity.

The status is a subresource which can be updated without updating the
whole resource and is a flat key-value list (or should be)
of properties to represent the state of a resource.

## Write Entities

A custom entity could be:

```csharp
class FooSpec
{
    public string? Test { get; set; }
}

[KubernetesEntity(Group = "test", ApiVersion = "v1")]
public class Foo : CustomKubernetesEntity<FooSpec>
{
}
```

Now a CRD for your "Foo" class is generated on build
or via the cli commands.

If you don't use the <xref:KubeOps.Operator.Entities.CustomKubernetesEntity`1> base class, you need to - at least - use the appropriate interfaces from `k8s`:

- `KubernetesObject`
- `IKubernetesObject<V1ObjectMeta>`

### Ignoring Entities

There are use-cases when you want to model / watch a custom entity from another
software engineer that are not part of the base models in `k8s`.

To prevent the generator from creating yamls for CRDs you don't own, use
the <xref:KubeOps.Operator.Entities.Annotations.IgnoreEntityAttribute>.

So as an example, one could try to watch for Ambassador-Mappings with
the following entity:

```csharp
public class MappingSpec
{
    public string Host { get; set; }
}

[IgnoreEntity]
[KubernetesEntity(Group = "getambassador.io", ApiVersion = "v2")]
public class Mapping : CustomKubernetesEntity<MappingSpec>
{
}
```

You need it to be a `KubernetesEntity` and a `IKubernetesObject<V1ObjectMeta>`, but
you don't want a CRD generated for it (thus the `IgnoreEntity` attribute).

## RBAC

The operator (SDK) will generate the role config for your
operator to be installed. When your operator needs access to
Kubernetes objects, they must be mentioned with the
RBAC attributes. During build, the SDK scans the configured
types and generates the RBAC role that the operator needs
to function.

There exist two versions of the attribute:
<xref:KubeOps.Operator.Rbac.EntityRbacAttribute> and
<xref:KubeOps.Operator.Rbac.GenericRbacAttribute>.

The generic RBAC attribute will be translated into a `V1PolicyRole`
according to the properties set in the attribute.

```csharp
[GenericRbac(Groups = new []{"apps"}, Resources = new[]{"deployments"}, Verbs = RbacVerb.All)]
```

The entity RBAC attribute is the elegant option to use
dotnet mechanisms. The CRD information is generated out of
the given types and then grouped by type and used RBAC verbs.
If you create multiple attributes with the same type, they are
concatenated.

```csharp
[EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Get | RbacVerb.Update)]
```

## Validation

During CRD generation, the generated json schema uses the types
of the properties to create the openApi schema.

You can use the various validator attributes to customize your crd:

(all attributes are on properties with the exception of the Description)

- `Description`: Describe the property or class
- `ExternalDocs`: Add a link to an external documentation
- `Items`: Customize MinItems / MaxItems and if the items should be unique
- `Lenght`: Customize the length of something
- `MultipleOf`: A number should be a multiple of
- `Pattern`: A valid ECMA script regex (e.g. `/\d*/`)
- `RangeMaximum`: The maximum of a value (with option to exclude the max itself)
- `RangeMinimum`: The minimum of a value (with option to exclude the min itself)
- `Required`: The field is listed in the required fields
- `PreserveUnknownFields`: Set the `X-Kubernetes-Preserve-Unknown-Fields` to `true`

> [!NOTE]
> For `Description`: if your project generates the XML documentation files
> for the result, the crd generator also searches for those files and a possible
> `<summary>` tag in the xml documentation. The attribute will take precedence though.

```csharp
public class MappingSpec
{
    /// <summary>This is a comment.</summary>
    [Description("This is another comment")]
    public string Host { get; set; }
}
```

In the example above, the text of the attribute will be used.

## Multi-Version Entities

You can manage multiple versions of a CRD. To do this, you can
specify multiple classes as the "same" entity, but with different
versions.

To mark multiple entity classes as the same, use exactly the same
`Kind`, `Group` and `PluralName` and differ in the `ApiVersion`
field.

### Version priority

Sorting of the versions - and therefore determine which version should be
the `storage version` if no attribute is provided - is done by the kubernetes
rules of version sorting:

Priority is as follows:

1. General Availablility (i.e. `V1Foobar`, `V2Foobar`)
2. Beta Versions (i.e. `V11Beta13Foobar`, `V2Beta1Foobar`)
3. Alpha Versions (i.e. `V16Alpha13Foobar`, `V2Alpha10Foobar`)

The parsed version numbers are sorted by the highest first, this leads
to the following version priority:

```
- v10
- v2
- v1
- v11beta2
- v10beta3
- v3beta1
- v12alpha1
- v11alpha2
```

This can also be seen over at the
[kubernetes documentation](https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definition-versioning/#version-priority).

### Storage Version

To determine the storage version (of which one, and exactly one must exist)
the system uses the previously mentioned version priority to sort the versions
and picking the first one. To overwrite this behaviour, use the
<xref:KubeOps.Operator.Entities.Annotations.StorageVersionAttribute>.

> [!WARNING]
> when multiple <xref:KubeOps.Operator.Entities.Annotations.StorageVersionAttribute>
> are used, the system will thrown an error.

To overwrite a version, annotate the entity class with the attribute.

### Example

#### Normal multiversion entity

Note that the `Kind`

```csharp
[KubernetesEntity(
    ApiVersion = "v1",
    Kind = "VersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "versionedentities")]
public class V1VersionedEntity : CustomKubernetesEntity
{
}

[KubernetesEntity(
    ApiVersion = "v1beta1",
    Kind = "VersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "versionedentities")]
public class V1Beta1VersionedEntity : CustomKubernetesEntity
{
}

[KubernetesEntity(
    ApiVersion = "v1alpha1",
    Kind = "VersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "versionedentities")]
public class V1Alpha1VersionedEntity : CustomKubernetesEntity
{
}
```

The resulting storage version would be `V1VersionedEntity`.

#### Overwritten storage version multiversion entity

```csharp
[KubernetesEntity(
    ApiVersion = "v1",
    Kind = "AttributeVersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "attributeversionedentities")]
[StorageVersion]
public class V1AttributeVersionedEntity : CustomKubernetesEntity
{
}

[KubernetesEntity(
    ApiVersion = "v2",
    Kind = "AttributeVersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "attributeversionedentities")]
public class V2AttributeVersionedEntity : CustomKubernetesEntity
{
}
```

The resulting storage version would be `V1AttributeVersionedEntity`.
