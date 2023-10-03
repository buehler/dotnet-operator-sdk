using KubeOps.Abstractions.Builder;
using KubeOps.Operator.Builder;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Extensions;

/// <summary>
/// Method extensions for the <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add the Kubernetes operator to the dependency injection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/>.</param>
    /// <returns>An <see cref="IOperatorBuilder"/> for further configuration and chaining.</returns>
    public static IOperatorBuilder AddKubernetesOperator(
        this IServiceCollection services) => new OperatorBuilder(services);
}
