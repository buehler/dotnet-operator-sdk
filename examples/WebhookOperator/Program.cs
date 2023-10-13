using KubeOps.Operator;
using KubeOps.Operator.Web.Builder;

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
