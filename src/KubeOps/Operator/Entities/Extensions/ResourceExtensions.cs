using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Finalizer;
using System.Linq;
using System.Threading.Tasks;

namespace KubeOps.Operator.Entities.Extensions
{
    public static class ResourceExtensions
    {
        public static Task RegisterFinalizer<TFinalizer, TResource>(
            this IKubernetesObject<V1ObjectMeta> resource, ResourceServices<TResource> services)
            where TFinalizer : IResourceFinalizer<TResource>
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var finalizer = services.Finalizers.Value
                .First(f => f.GetType() == typeof(TFinalizer));
            return finalizer.Register((TResource)resource);
        }
    }
}
