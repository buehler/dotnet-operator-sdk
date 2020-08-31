using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Finalizer
{
    public interface IResourceFinalizer
    {
        string Identifier { get; }
    }

    public interface IResourceFinalizer<in TResource> : IResourceFinalizer
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        Task Register(TResource resource);

        internal Task FinalizeResource(TResource resource);
    }
}
