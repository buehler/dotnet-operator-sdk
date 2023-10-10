using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using KubeOps.Operator;

using Microsoft.AspNetCore.HttpOverrides;

using WebhookOperator.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddKubernetesOperator()
    .RegisterComponents();

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// app.UseForwardedHeaders(
//     new()
//     {
//         ForwardedHeaders = ForwardedHeaders.XForwardedProto,
//         KnownNetworks = { new IPNetwork(IPAddress.Parse("0.0.0.0"), 0) },
//     });
app.UseRouting();
app.UseDeveloperExceptionPage();
app.MapControllers();

await app.RunAsync();
