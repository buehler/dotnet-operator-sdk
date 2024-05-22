using KubeOps.Operator;
using KubeOps.Operator.Web.Builder;

#pragma warning disable CS0618 // Type or member is obsolete

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddKubernetesOperator()
    .RegisterComponents()
#if DEBUG
    .AddDevelopmentTunnel(5000)
#endif
    ;

builder.Services
    .AddControllers();

var app = builder.Build();

app.UseRouting();
app.UseDeveloperExceptionPage();
app.MapControllers();

await app.RunAsync();
