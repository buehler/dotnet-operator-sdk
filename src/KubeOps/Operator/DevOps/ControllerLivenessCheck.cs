using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KubeOps.Operator.Controller;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.DevOps
{
    internal class ControllerLivenessCheck : IHealthCheck
    {
        private readonly IList<IResourceController> _controller;

        public ControllerLivenessCheck(IEnumerable<IHostedService> services)
        {
            _controller = services
                .Where(s => s is IResourceController)
                .OfType<IResourceController>()
                .ToList();
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (_controller.All(c => c.Running))
            {
                return Task.FromResult(HealthCheckResult.Healthy("all controllers are running."));
            }

            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "some controllers are not running.",
                    data: _controller.ToDictionary(
                        c => c.GetType().Name,
                        c => $"running: {c.Running}" as object)));
        }
    }
}
