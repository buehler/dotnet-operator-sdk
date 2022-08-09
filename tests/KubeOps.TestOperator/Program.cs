using KubeOps.Operator;
using KubeOps.TestOperator.TestManager;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddKubernetesOperator(s => s.EnableLeaderElection = false);
//.AddWebhookLocaltunnel();
builder.Services.AddTransient<IManager, TestManager>();

var app = builder.Build();
app.UseKubernetesOperator();
await app.RunOperatorAsync(args);
