using k8s;
using k8s.Models;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Controller
{
    /// <summary>
    /// Resource controller interface.
    /// This interface is primarily used for generic type help.
    /// </summary>
    public interface IResourceController : IHostedService
    {
        internal bool Running { get; }
    }

    /// <summary>
    /// Generic resource controller interface.
    /// This interface is primarily used for generic type help.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface IResourceController<TEntity> : IResourceController
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
    }
}
