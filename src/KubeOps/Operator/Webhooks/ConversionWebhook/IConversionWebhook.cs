using k8s;
using k8s.Models;

namespace KubeOps.Operator.Webhooks.ConversionWebhook;

public interface IConversionWebhook<in TIn, out TOut>
    where TIn : IKubernetesObject<V1ObjectMeta>
    where TOut : IKubernetesObject<V1ObjectMeta>
{
    public TOut Convert(TIn customResourceInput);
}
