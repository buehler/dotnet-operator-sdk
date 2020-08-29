using k8s;
using k8s.Models;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Controller
{
    public interface IResourceController : IHostedService
    {
        internal bool Running { get; }
    }

    public interface IResourceController<TEntity> : IResourceController
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
    }
}
