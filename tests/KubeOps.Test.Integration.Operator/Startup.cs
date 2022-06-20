using KubeOps.Operator;

namespace KubeOps.Test.Integration.Operator;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddKubernetesOperator()
            .AddEntity<V1TestEntity>()
            .AddEntity<V2TestEntity>()
            .AddController<TestController>()
            .AddConversionWebhook<V1ToV2TestConversionWebhook>()
            .AddConversionWebhook<V2ToV1TestConversionWebhook>();

    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseKubernetesOperator();
    }
}
