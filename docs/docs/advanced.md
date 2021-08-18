# Advanced Topics

## Assembly Scanning

By default, KubeOps scans the assembly containing the main entrypoint for 
controller, finalizer, webhook and entity types, and automatically registers
all types that implement the correct interfaces for usage.

If some of the above are stored in a different assembly, KubeOps must be
specifically instructed to scan that assembly <xref:KubeOps.Operator.Builder.IOperatorBuilder.AddResourceAssembly(System.Reflection.Assembly)> or else those types won't be loaded.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddKubernetesOperator()
            .AddResourceAssembly(typeof(CustomEntityController).Assembly)
    }
    
    public void Configure(IApplicationBuilder app)
    {
        app.UseKubernetesOperator();
    }
}
```

## Manual Registration

If desired, the default behavior of assembly scanning can be disabled so
specific components can be registered manually. (Using both methods in parallel
is supported, such as if you want to load all components from one assembly and
only some from another.)

See <xref:KubeOps.Operator.Builder.IOperatorBuilder> for details on the methods
utilized in this registration pattern.

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddKubernetesOperator(settings =>
            {
                settings.EnableAssemblyScanning = false;
            })
            .AddEntity<V1DemoEntityClone>()
            .AddController<DemoController, V1DemoEntityClone>()
            .AddController<DemoControllerClone>()
            .AddFinalizer<DemoFinalizer>()
            .AddValidationWebhook<DemoValidator>()
            .AddMutationWebhook<DemoMutator>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseKubernetesOperator();
    }
}
```