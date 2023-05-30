namespace KubeOps.Operator.Controller;

public interface IManagedResourceController : IDisposable
{
    Task StartAsync();

    Task StopAsync();
}
