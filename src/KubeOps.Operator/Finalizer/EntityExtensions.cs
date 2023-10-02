using k8s;
using k8s.Models;

namespace KubeOps.Operator.Finalizer;

public static class EntityExtensions
{
    public static async Task AttachFinalizer<TEntity, TFinalizer>(this IKubernetesObject<V1ObjectMeta> entity)
    {
        
    }
}
