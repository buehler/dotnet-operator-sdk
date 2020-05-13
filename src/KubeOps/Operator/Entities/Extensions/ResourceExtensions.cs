using System.Linq;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using KubeOps.Operator.DependencyInjection;
using KubeOps.Operator.Finalizer;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Entities.Extensions
{
    public static class ResourceExtensions
    {
        public static Task RegisterFinalizer<TFinalizer, TResource>(
            this IKubernetesObject<V1ObjectMeta> resource)
            where TFinalizer : IResourceFinalizer<TResource>
            where TResource : IKubernetesObject<V1ObjectMeta>
        {
            var finalizer = DependencyInjector.Services
                .GetServices<IResourceFinalizer<TResource>>()
                .First(f => f.GetType() == typeof(TFinalizer));
            return finalizer.Register((TResource) resource);
        }
    }
}
