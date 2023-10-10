using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using KubeOps.Operator;

using Microsoft.AspNetCore.HttpOverrides;

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

app.UseRouting();
app.UseDeveloperExceptionPage();
app.MapControllers();

await app.RunAsync();
