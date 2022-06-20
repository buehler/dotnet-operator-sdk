using KubeOps.Operator;

namespace KubeOps.Test.Integration.Operator;

public class Program
{
    static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }

    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args).Build().RunOperatorAsync(args);
    }
}
