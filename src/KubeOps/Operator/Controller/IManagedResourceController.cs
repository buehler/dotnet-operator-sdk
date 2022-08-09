namespace KubeOps.Operator.Controller;

internal interface IManagedResourceController : IDisposable
{
    Task StartAsync();

    Task StopAsync();
}
