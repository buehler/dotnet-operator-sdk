using KubeOps.Operator;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddKubernetesOperator()
    .RegisterComponents();

builder.Services
    .AddControllers();

var app = builder.Build();

app.UseRouting();
app.UseDeveloperExceptionPage();
app.MapControllers();

await app.RunAsync();
