using KubeOps.Operator;
using KubeOps.Test.Integration.Operator;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace KubeOps.Test.Integration;

internal class TestOperatorRunner : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        host.RunOperatorAsync(new []{"install"});
        return host;
    }
}
