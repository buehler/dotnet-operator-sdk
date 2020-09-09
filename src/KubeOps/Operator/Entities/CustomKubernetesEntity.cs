using k8s;
using k8s.Models;

namespace KubeOps.Operator.Entities
{
    /// <summary>
    /// Defines a custom kubernetes entity which can be used in finalizers and controllers.
    /// </summary>
    public abstract class CustomKubernetesEntity : KubernetesObject, IKubernetesObject<V1ObjectMeta>
    {
        public V1ObjectMeta Metadata { get; set; } = new V1ObjectMeta();
    }
}
