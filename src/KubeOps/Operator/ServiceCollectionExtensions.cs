using System;
using KubeOps.Operator.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator
{
    public static class ServiceCollectionExtensions
    {
        public static IOperatorBuilder AddKubernetesOperator(
            this IServiceCollection services,
            Action<OperatorSettings>? configure)
        {
            var settings = new OperatorSettings();
            configure?.Invoke(settings);
            return AddKubernetesOperator(services, settings);
        }

        public static IOperatorBuilder AddKubernetesOperator(
            this IServiceCollection services,
            OperatorSettings settings) => new OperatorBuilder(services).AddOperatorBase(settings);
    }
}
