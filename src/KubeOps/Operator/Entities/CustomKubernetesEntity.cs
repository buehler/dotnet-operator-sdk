using System.Collections.Generic;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Entities
{
    public abstract class CustomKubernetesEntity : KubernetesObject, IKubernetesObject<V1ObjectMeta>
    {
        public V1ObjectMeta Metadata { get; set; } = new V1ObjectMeta();
    }

    public abstract class CustomKubernetesEntity<TSpec> : CustomKubernetesEntity, ISpec<TSpec>
        where TSpec : new()
    {
        public TSpec Spec { get; set; } = new TSpec();
    }

    public abstract class CustomKubernetesEntity<TSpec, TStatus> : CustomKubernetesEntity<TSpec>, IStatus<TStatus>
        where TSpec : new()
        where TStatus : new()
    {
        public TStatus Status { get; set; } = new TStatus();
    }

    public class EntityList<T> : KubernetesObject
        where T : IKubernetesObject<V1ObjectMeta>
    {
        public V1ListMeta Metadata { get; set; } = new V1ListMeta();
        public IList<T> Items { get; set; } = new List<T>();
    }
}
