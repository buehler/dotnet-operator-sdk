using System.Diagnostics.CodeAnalysis;
using k8s;
using k8s.Models;

namespace KubeOps.Operator.Entities
{
    [SuppressMessage(
        "StyleCop.CSharp.MaintainabilityRules",
        "SA1402:FileMayOnlyContainASingleType",
        Justification = "This naming of generics should appear in the same file.")]
    public abstract class CustomKubernetesEntity : KubernetesObject, IKubernetesObject<V1ObjectMeta>
    {
        public V1ObjectMeta Metadata { get; set; } = new V1ObjectMeta();
    }

    [SuppressMessage(
        "StyleCop.CSharp.MaintainabilityRules",
        "SA1402:FileMayOnlyContainASingleType",
        Justification = "This naming of generics should appear in the same file.")]
    public abstract class CustomKubernetesEntity<TSpec> : CustomKubernetesEntity, ISpec<TSpec>
        where TSpec : new()
    {
        public TSpec Spec { get; set; } = new TSpec();
    }

    [SuppressMessage(
        "StyleCop.CSharp.MaintainabilityRules",
        "SA1402:FileMayOnlyContainASingleType",
        Justification = "This naming of generics should appear in the same file.")]
    public abstract class CustomKubernetesEntity<TSpec, TStatus> : CustomKubernetesEntity<TSpec>, IStatus<TStatus>
        where TSpec : new()
        where TStatus : new()
    {
        public TStatus Status { get; set; } = new TStatus();
    }
}
