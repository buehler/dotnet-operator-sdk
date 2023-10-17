using k8s;
using k8s.Models;

using KubeOps.Transpiler;

namespace KubeOps.Operator.Web.Webhooks.Conversion;

public interface IEntityConverter
{
    Type FromType { get; }

    Type ToType { get; }

    string FromGroupVersion { get; }

    string ToGroupVersion { get; }

    object Convert(object from);

    object Revert(object to);
}

public interface IEntityConverter<TFrom, TTo> : IEntityConverter
    where TFrom : IKubernetesObject<V1ObjectMeta>
    where TTo : IKubernetesObject<V1ObjectMeta>
{
    Type IEntityConverter.FromType => typeof(TFrom);

    Type IEntityConverter.ToType => typeof(TTo);

    string IEntityConverter.FromGroupVersion => Entities.ToEntityMetadata<TFrom>().Metadata.GroupWithVersion;

    string IEntityConverter.ToGroupVersion => Entities.ToEntityMetadata<TTo>().Metadata.GroupWithVersion;

    TTo Convert(TFrom from);

    TFrom Revert(TTo to);

    object IEntityConverter.Convert(object from)
        => Convert((TFrom)from);

    object IEntityConverter.Revert(object to)
        => Revert((TTo)to);
}
