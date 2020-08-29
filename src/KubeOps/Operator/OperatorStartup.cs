using KubeOps.Operator.DevOps;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace KubeOps.Operator
{
    public class OperatorStartup
    {
        internal const string LivenessTag = "liveness";
        internal const string ReadinessTag = "readiness";

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddHealthChecks()
                .ForwardToPrometheus();

            services.AddHealthCheck<ControllerLivenessCheck>();
        }

        public void Configure(IApplicationBuilder app, OperatorSettings settings)
        {
            app.UseRouting();
            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapHealthChecks(
                        settings.LivenessEndpoint,
                        new HealthCheckOptions { Predicate = reg => reg.Tags.Contains(LivenessTag) });
                    endpoints.MapHealthChecks(
                        settings.ReadinessEndpoint,
                        new HealthCheckOptions { Predicate = reg => reg.Tags.Contains(ReadinessTag) });

                    endpoints.MapMetrics(settings.MetricsEndpoint);
                });
        }
    }
}
