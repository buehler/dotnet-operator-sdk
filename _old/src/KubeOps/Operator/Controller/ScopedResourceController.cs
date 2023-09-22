namespace KubeOps.Operator.Controller;

internal class ScopedResourceController : IManagedResourceController
{
    private readonly IDisposable _scope;
    private readonly IManagedResourceController _controller;

    public ScopedResourceController(IDisposable scope, IManagedResourceController controller)
    {
        _scope = scope;
        _controller = controller;
    }

    public void Dispose() => _scope.Dispose();

    public async Task StartAsync() => await _controller.StartAsync();

    public async Task StopAsync() => await _controller.StopAsync();
}
