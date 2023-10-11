# KubeOps Operator Web

The KubeOps Operator Web package provides a webserver to enable
webhooks for your Kubernetes operator.

## Usage

To enable webhooks and external access to your operator, you need to
use ASP.net. The project file needs to reference `Microsoft.NET.Sdk.Web`
instead of `Microsoft.NET.Sdk` and the `Program.cs` needs to be changed.

To allow webhooks, the MVC controllers need to be registered and mapped.

The basic `Program.cs` setup looks like this:
```csharp
using KubeOps.Operator;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddKubernetesOperator()
    .RegisterComponents();

builder.Services
    .AddControllers();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

await app.RunAsync();
```

Note the `.AddControllers` and `.MapControllers` call.
Without them, your webhooks will not be reachable.

## Validation Hooks

## Mutation Hooks

TODO.

## Conversion Hooks

TODO.

## Installing In The Cluster

TODO.
