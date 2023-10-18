using System.Runtime.Versioning;

using k8s;
using k8s.Models;

using KubeOps.Transpiler;

namespace KubeOps.Operator.Web.Webhooks.Conversion;

[RequiresPreviewFeatures(
    "Conversion webhooks API is not yet stable, the way that conversion " +
    "webhooks are implemented may change in the future based on user feedback.")]
public interface IEntityConverter<TTarget>
    where TTarget : IKubernetesObject<V1ObjectMeta>
{
    Type FromType { get; }

    Type ToType => typeof(TTarget);

    string FromGroupVersion { get; }

    string ToGroupVersion => Entities.ToEntityMetadata<TTarget>().Metadata.GroupWithVersion;

    TTarget Convert(object from);

    object Revert(TTarget to);
}

[RequiresPreviewFeatures(
    "Conversion webhooks API is not yet stable, the way that conversion " +
    "webhooks are implemented may change in the future based on user feedback.")]
public interface IEntityConverter<TFrom, TTo> : IEntityConverter<TTo>
    where TFrom : IKubernetesObject<V1ObjectMeta>
    where TTo : IKubernetesObject<V1ObjectMeta>
{
    Type IEntityConverter<TTo>.FromType => typeof(TFrom);

    string IEntityConverter<TTo>.FromGroupVersion => Entities.ToEntityMetadata<TFrom>().Metadata.GroupWithVersion;

    TTo Convert(TFrom from);

    new TFrom Revert(TTo from);

    TTo IEntityConverter<TTo>.Convert(object from)
        => Convert((TFrom)from);

    object IEntityConverter<TTo>.Revert(TTo to)
        => Revert(to);
}
