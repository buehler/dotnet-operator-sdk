using KubeOps.Operator.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace KubeOps.Operator
{
    public static class ApplicationBuilderExtensions
    {
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
                });
        }
    }
}
