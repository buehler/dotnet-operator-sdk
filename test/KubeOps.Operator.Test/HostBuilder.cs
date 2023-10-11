using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Test;

public sealed class HostBuilder : IAsyncDisposable
{
    private IHost? _host;
    private bool _isRunning;

    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException();

    public async Task ConfigureAndStart(Action<HostApplicationBuilder> configure)
    {
        if (_host is not null && _isRunning)
        {
            return;
        }

        var builder = Host.CreateApplicationBuilder();
#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Trace);
#else
        builder.Logging.SetMinimumLevel(LogLevel.None);
#endif
        configure(builder);
        _host = builder.Build();
        await _host.StartAsync();
        _isRunning = true;
    }

    public async ValueTask DisposeAsync()
    {
        _isRunning = false;
        if (_host is null)
        {
            return;
        }

        await _host.StopAsync();
        _host.Dispose();
    }
}
