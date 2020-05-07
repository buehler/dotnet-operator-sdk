using System.Linq;
using System.Threading.Tasks;
using Dos.Operator.DependencyInjection;
using Dos.Operator.Finalizer;
using k8s;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Dos.Operator.Entities
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
