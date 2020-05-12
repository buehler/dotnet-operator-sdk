using k8s;
using k8s.Models;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Controller
{
    public interface IResourceController<TEntity> : IHostedService
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
    }
}
