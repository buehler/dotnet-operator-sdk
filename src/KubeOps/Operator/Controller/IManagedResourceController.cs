using System;
using System.Threading.Tasks;
using KubeOps.Operator.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Controller;

internal interface IManagedResourceController : IDisposable
{
    Task StartAsync();

    Task StopAsync();
}
