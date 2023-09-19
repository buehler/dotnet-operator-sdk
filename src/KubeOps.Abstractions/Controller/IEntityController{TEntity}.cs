using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Controller;

public interface IEntityController<in TEntity>
where TEntity : IKubernetesObject<V1ObjectMeta>
{
    
}
