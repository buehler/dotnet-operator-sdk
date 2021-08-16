using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KubeOps.Operator.Leadership;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Operator.Controller
{
    internal class ResourceControllerManager : IHostedService
    {
        private readonly IControllerInstanceBuilder _controllerInstanceBuilder;
        private readonly ILeaderElection _leaderElection;
        private readonly List<IManagedResourceController> _controllerList;

        private IDisposable? _leadershipSubscription;

        public ResourceControllerManager(
            IControllerInstanceBuilder controllerInstanceBuilder,
            ILeaderElection leaderElection)
        {
            _controllerInstanceBuilder = controllerInstanceBuilder;
            _leaderElection = leaderElection;
            _controllerList = new List<IManagedResourceController>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _controllerList.AddRange(_controllerInstanceBuilder.MakeManagedControllers());

            _leadershipSubscription = _leaderElection.LeadershipChange.Subscribe(LeadershipChanged);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _leadershipSubscription?.Dispose();
            foreach (var controller in _controllerList)
            {
                controller.StopAsync();
                controller.Dispose();
            }

            _controllerList.Clear();
            return Task.CompletedTask;
        }

        private void LeadershipChanged(LeaderState state)
        {
            if (state == LeaderState.None)
            {
                return;
            }

            foreach (var controller in _controllerList)
            {
                if (state == LeaderState.Leader)
                {
                    controller.StartAsync();
                }
                else
                {
                    controller.StopAsync();
                }
            }
        }
    }
}
