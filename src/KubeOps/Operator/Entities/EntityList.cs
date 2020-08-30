using System.Collections.Generic;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Entities
{
    public class EntityList<T> : KubernetesObject
        where T : IKubernetesObject<V1ObjectMeta>
    {
        public V1ListMeta Metadata { get; set; } = new V1ListMeta();

        public IList<T> Items { get; set; } = new List<T>();
    }
}
