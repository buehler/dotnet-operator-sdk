using k8s.Models;

using KubeOps.Abstractions.Entities.Attributes;

namespace KubeOps.Transpiler.Test.TestEntities;

[KubernetesEntity(
    ApiVersion = "v1alpha1",
    Kind = "VersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "versionedentities")]
public class V1Alpha1VersionedEntity : Base
{
}

[KubernetesEntity(
    ApiVersion = "v1beta1",
    Kind = "VersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "versionedentities")]
public class V1Beta1VersionedEntity : Base
{
}

[KubernetesEntity(
    ApiVersion = "v2beta2",
    Kind = "VersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "versionedentities")]
public class V2Beta2VersionedEntity : Base
{
}

[KubernetesEntity(
    ApiVersion = "v2",
    Kind = "VersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "versionedentities")]
public class V2VersionedEntity : Base
{
}

[KubernetesEntity(
    ApiVersion = "v1",
    Kind = "VersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "versionedentities")]
public class V1VersionedEntity : Base
{
}

[KubernetesEntity(
    ApiVersion = "v1",
    Kind = "AttributeVersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "attributeversionedentities")]
[StorageVersion]
public class V1AttributeVersionedEntity : Base
{
}

[KubernetesEntity(
    ApiVersion = "v2",
    Kind = "AttributeVersionedEntity",
    Group = "kubeops.test.dev",
    PluralName = "attributeversionedentities")]
public class V2AttributeVersionedEntity : Base
{
}
