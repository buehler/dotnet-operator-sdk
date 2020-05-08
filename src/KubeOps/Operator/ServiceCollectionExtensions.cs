using Dos.Operator.Controller;
using Dos.Operator.Finalizer;
using k8s;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Dos.Operator
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddResourceController<TController, TEntity>(this IServiceCollection services)
            where TController : ResourceControllerBase<TEntity>
            where TEntity : IKubernetesObject<V1ObjectMeta> =>
            services.AddHostedService<TController>();

        public static IServiceCollection AddResourceFinalizer<TFinalizer, TEntity>(this IServiceCollection services)
            where TFinalizer : ResourceFinalizerBase<TEntity>
            where TEntity : IKubernetesObject<V1ObjectMeta> =>
            services.AddTransient<IResourceFinalizer<TEntity>, TFinalizer>();
    }
}
