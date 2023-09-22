using k8s;

namespace KubeOps.Operator.Entities;

/// <summary>
/// Defines a custom kubernetes entity which can be used in finalizers and controllers.
/// This entity contains a <see cref="Spec"/>, which means in contains specified data.
/// </summary>
/// <typeparam name="TSpec">The type of the specified data.</typeparam>
public abstract class CustomKubernetesEntity<TSpec> : CustomKubernetesEntity, ISpec<TSpec>
    where TSpec : new()
{
    /// <summary>
    /// Specification of the kubernetes object.
    /// </summary>
    public TSpec Spec { get; set; } = new();
}
