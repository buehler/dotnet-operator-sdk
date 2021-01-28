using System.Threading.Tasks;
using DotnetKubernetesClient.Entities;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Finalizer
{
    /// <summary>
    /// Finalizer for a resource.
    /// </summary>
    /// <typeparam name="TResource">The type of the resources (entities).</typeparam>
    public interface IResourceFinalizer<in TResource>
        where TResource : IKubernetesObject<V1ObjectMeta>
    {
        private const byte MaxNameLength = 254;

        /// <summary>
        /// Unique identifier for this finalizer.
        /// Defaults to `"{Typename}.{crd.Singular}.finalizers.{crd.Group}"`.
        /// </summary>
        /// <example>testentityfinalizer.test.finalizer.dev.</example>
        string Identifier
        {
            get
            {
                var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TResource>();
                var name = $"{GetType().Name.ToLowerInvariant()}.{crd.Singular}.finalizers.{crd.Group}";
                return name.Length > MaxNameLength
                    ? name.Substring(0, MaxNameLength)
                    : name;
            }
        }

        /// <summary>
        /// Finalize a resource that is pending for deletion.
        /// </summary>
        /// <param name="resource">The kubernetes resource that needs to be finalized.</param>
        /// <returns>A task for when the operation is done.</returns>
        Task FinalizeAsync(TResource resource);
    }
}
