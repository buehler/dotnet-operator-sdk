using System;
using System.Threading.Tasks;

namespace KubeOps.Operator.Controller
{
    internal interface IManagedResourceController : IDisposable
    {
        Type ControllerType { get; set; }

        Task Start();

        Task Stop();
    }
}
