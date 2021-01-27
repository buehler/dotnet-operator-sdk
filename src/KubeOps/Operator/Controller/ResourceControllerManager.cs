using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Leadership;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Controller
{
    internal class ResourceControllerManager : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILeaderElection _leaderElection;

        private readonly List<IManagedResourceController> _controller = new();

        private IDisposable? _leadershipSubscription;

        public ResourceControllerManager(IServiceProvider services, ILeaderElection leaderElection)
        {
            _services = services;
            _leaderElection = leaderElection;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var (controllerType, entityType) in OperatorBuilder.GetControllers())
            {
                var managedType = typeof(ManagedResourceController<>).MakeGenericType(entityType);
                var managedInstance = _services.GetRequiredService(managedType) as IManagedResourceController ??
                                      throw new Exception(
                                          $"Could not create managed controller with type {managedType}.");
                managedInstance.ControllerType = controllerType;
                _controller.Add(managedInstance);
            }

            _leadershipSubscription = _leaderElection.LeadershipChange.Subscribe(LeadershipChanged);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _leadershipSubscription?.Dispose();
            foreach (var controller in _controller)
            {
                controller.Stop();
                controller.Dispose();
            }

            _controller.Clear();
            return Task.CompletedTask;
        }

        private void LeadershipChanged(LeaderState state)
        {
            if (state == LeaderState.None)
            {
                return;
            }

            foreach (var controller in _controller)
            {
                if (state == LeaderState.Leader)
                {
                    controller.Start();
                }
                else
                {
                    controller.Stop();
                }
            }
        }
    }
}
