using System.Linq;
using System.Reflection;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Services;
using KubeOps.Operator.Webhooks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace KubeOps.Operator
{
    /// <summary>
    /// Extensions for the <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Use the kubernetes operator.
        /// Register routing (.UseRouting()) and endpoints.
        /// The endpoints contain health-checks and metrics endpoints.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        public static void UseKubernetesOperator(
            this IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(
                endpoints =>
                {
                    var settings = app.ApplicationServices.GetRequiredService<OperatorSettings>();

                    endpoints.MapHealthChecks(
                        settings.LivenessEndpoint,
                        new HealthCheckOptions { Predicate = reg => reg.Tags.Contains(OperatorBuilder.LivenessTag) });
                    endpoints.MapHealthChecks(
                        settings.ReadinessEndpoint,
                        new HealthCheckOptions { Predicate = reg => reg.Tags.Contains(OperatorBuilder.ReadinessTag) });

                    endpoints.MapMetrics(settings.MetricsEndpoint);

                    var logger = app.ApplicationServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("ApplicationStartup");

                    using var scope = app.ApplicationServices.CreateScope();
                    var locator = scope.ServiceProvider.GetRequiredService<ResourceLocator>();

                    foreach (var (validatorType, resourceType) in locator.ValidatorTypes)
                    {
                        var validator = scope.ServiceProvider.GetRequiredService(validatorType);
                        var registerMethod = typeof(IAdmissionWebhook<,>)
                            .MakeGenericType(resourceType, typeof(ValidationResult))
                            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                            .First(m => m.Name == "Register");
                        registerMethod.Invoke(validator, new object[] { endpoints });
                        var (name, endpoint) = Webhooks.Webhooks.Metadata<ValidationResult>(validator, resourceType);
                        logger.LogInformation(
                            @"Registered validation webhook ""{name}"" under ""{endpoint}"".",
                            name,
                            endpoint);
                    }

                    foreach (var (mutatorType, resourceType) in locator.MutatorTypes)
                    {
                        var mutator = scope.ServiceProvider.GetRequiredService(mutatorType);
                        var registerMethod = typeof(IAdmissionWebhook<,>)
                            .MakeGenericType(resourceType, typeof(MutationResult))
                            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                            .First(m => m.Name == "Register");
                        registerMethod.Invoke(mutator, new object[] { endpoints });
                        var (name, endpoint) = Webhooks.Webhooks.Metadata<MutationResult>(mutator, resourceType);
                        logger.LogInformation(
                            @"Registered mutation webhook ""{name}"" under ""{endpoint}"".",
                            name,
                            endpoint);
                    }
                });
        }
    }
}
