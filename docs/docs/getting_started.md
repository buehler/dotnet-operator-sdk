# Getting Started

This document should describe what steps you need to follow, to fire up your own operator.
This covers the basic installation of the operator sdk, further
clarification / documentation is in the specific sections.

The operator sdk is designed as an extension to the Generic Web Host of Microsoft.
So you'll find method extensions for `IServiceCollection` and `IApplicationBuilder`
that activate and start the operator as a web application.

## Terminology

- `Entity`: A (C#) model - an entity - that is used in kubernetes. An entity defines the CRD.
- `Resource`: An instance of an entity.
- `Controller` or `ResourceController`: An instance of a resource manager
  that is responsible for the reconciliation of an entity.
- `Finalizer`: A special resource manager that is attached to the entity
  via identifier. The finalizers are called when an entity is deleted
  on kubernetes.
- `CRD`: CustomResourceDefinition of kubernetes.

## How To Start

Using this sdk is pretty simple:

- Create a new asp.net core application
- Install the package
- Replace the `Run` function in `Program.cs`
- Add the operator to `Startup.cs`
- Write entities / controllers / finalizers
- Go.

> [!NOTE]
> If you don't create an asp.net core application (template)
> please note that the output type of the application must be an "exe":
> `<OutputType>Exe</OutputType>`

### Install the package

```bash
dotnet add package KubeOps
```

That's it.

### Replace the Run function

In your `Program.cs` file, replace `Build().Run()` with `Build().RunOperator(args)`:

```csharp
public static class Program
{
    public static Task<int> Main(string[] args) =>
        CreateHostBuilder(args)
            .Build()
            .RunOperator(args);

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
```

This adds the default commands (like run and the code generators) to your app.
The commands are documentated under the [CLI Commands](./commands.md) section.

> [!NOTE]
> Technically you don't need to replace the function,
> but if you don't, the other commands like yaml generation
> are not available to your application. Also namespaceing is not
> possible via run flag.

### Add to Startup.cs

```csharp
public class Startup
{
    /* snip... */
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddKubernetesOperator() // config / settings here
            .AddFinalizer<TestEntityFinalizer>()
            .AddController<TestController>(); // Add controllers / finalizers / ... here

        // your own dependencies
        services.AddTransient<IManager, TestManager.TestManager>();
    }

    public void Configure(IApplicationBuilder app)
    {
        // fire up the mappings for the operator
        // this is technically not needed, but if you don't call this
        // function, the healthchecks and mappings are not
        // mapped to endpoints (therefore not callable)
        app.UseKubernetesOperator();
    }
}
```

## Next Steps

When you completed this basic setup instructions, the next steps may or may not include:

- Writing entities: Find more under ["Entites"](./entities.md)
- Writing controllers: Find more under ["Controller"](./controller.md)
- Writing finalizer: Find more under ["Finalizer"](./finalizer.md)
- Writing webhooks: Find more under ["Webhooks"](./webhooks.md)
- Customize the operator: Find more under ["Settings"](./settings.md)
- Writing and using utilities: Find more under ["Utilities"](./utilities.md)
