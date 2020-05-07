using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace Dos.Operator.Finalizer
{
    public interface IResourceFinalizer<in TResource>
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        string Identifier { get; }

        Task Register(TResource resource);

        internal Task FinalizeResource(TResource resource);
    }
}
