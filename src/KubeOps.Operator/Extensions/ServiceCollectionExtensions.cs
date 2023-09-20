using KubeOps.Abstractions.Builder;
using KubeOps.Operator.Builder;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add the kubernetes operator to the dependency injection
    /// and configure the operator.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An optional configure action for adjusting settings in the operator.</param>
    /// <returns>An <see cref="IOperatorBuilder"/> for further configuration and chaining.</returns>
    // public static IOperatorBuilder AddKubernetesOperator(
    //     this IServiceCollection services,
    //     Action? configure = null)
    // {
    //     // var settings = new OperatorSettings();
    //     // configure?.Invoke(settings);
    //     return AddKubernetesOperator(services, settings);
    // }

    /// <summary>
    /// Add the kubernetes operator to the dependency injection
    /// and configure the operator.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>.</param>
    /// <param name="settings">An instance of the operator settings to use.</param>
    /// <returns>An <see cref="IOperatorBuilder"/> for further configuration and chaining.</returns>
    public static IOperatorBuilder AddKubernetesOperator(
        this IServiceCollection services) => new OperatorBuilder(services);
}
