﻿using System.Reflection;
using System.Runtime.InteropServices;

using k8s;
using k8s.Models;

using KubeOps.Operator.Web.Builder;
using KubeOps.Operator.Web.LocalTunnel;
using KubeOps.Operator.Web.Test.TestApp;
using KubeOps.Operator.Web.Webhooks;
using KubeOps.Transpiler;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace KubeOps.Operator.Web.Test;

[CollectionDefinition(Name, DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<CrdInstaller>, ICollectionFixture<ApplicationProvider>
{
    public const string Name = "Integration Tests";
}

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase;

public sealed class CrdInstaller : IAsyncLifetime
{
    private List<V1CustomResourceDefinition> _crds = [];

    public async Task InitializeAsync()
    {
        await using var p = new MlcProvider();
        await p.InitializeAsync();
        _crds = p.Mlc.Transpile(new[] { typeof(V1OperatorWebIntegrationTestEntity) }).ToList();

        using var client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        foreach (var crd in _crds)
        {
            switch (await client.ApiextensionsV1.ListCustomResourceDefinitionAsync(
                        fieldSelector: $"metadata.name={crd.Name()}"))
            {
                case { Items: [var existing] }:
                    crd.Metadata.ResourceVersion = existing.ResourceVersion();
                    await client.ApiextensionsV1.ReplaceCustomResourceDefinitionAsync(crd, crd.Name());
                    break;
                default:
                    await client.ApiextensionsV1.CreateCustomResourceDefinitionAsync(crd);
                    break;
            }
        }
    }

    public async Task DisposeAsync()
    {
        using var client = new Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        foreach (var crd in _crds)
        {
            await client.ApiextensionsV1.DeleteCustomResourceDefinitionAsync(crd.Name());
        }
    }
}

public sealed class MlcProvider : IAsyncLifetime
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    static MlcProvider()
    {
        MSBuildLocator.RegisterDefaults();
    }

    public MetadataLoadContext Mlc { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var assemblyConfigurationAttribute =
            typeof(MlcProvider).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();
        var buildConfigurationName = assemblyConfigurationAttribute?.Configuration ?? "Debug";

        try
        {
            await Semaphore.WaitAsync();
            using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                { "Configuration", buildConfigurationName },
            });
            workspace.SkipUnrecognizedProjects = true;
            workspace.LoadMetadataForReferencedProjects = true;
            var project = await workspace.OpenProjectAsync("../../../KubeOps.Operator.Web.Test.csproj");

            Mlc = ContextCreator.Create(Directory
                .GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")
                .Concat(Directory.GetFiles(Path.GetDirectoryName(project.OutputFilePath)!, "*.dll"))
                .Distinct(), coreAssemblyName: typeof(object).Assembly.GetName().Name);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public Task DisposeAsync()
    {
        Mlc.Dispose();
        return Task.CompletedTask;
    }
}

public sealed class ApplicationProvider : IAsyncLifetime
{
    private WebApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(c => c.ListenAnyIP(5000));
        builder.Services
            .AddKubernetesOperator()
            .AddDevelopmentTunnel(5000);

        builder.Services.AddControllers().AddApplicationPart(typeof(ApplicationProvider).Assembly);
        builder.Services.AddHealthChecks();

        builder.Services.AddLogging(c =>
        {
            c.AddSimpleConsole();
#if DEBUG
            c.SetMinimumLevel(LogLevel.Trace);
#else
                c.SetMinimumLevel(LogLevel.None);
#endif
        });

        builder.Services.RemoveAll<WebhookLoader>();
        builder.Services.AddSingleton(new WebhookLoader(typeof(ApplicationProvider).Assembly));

        _app = builder.Build();

        _app.UseRouting();
        _app.UseDeveloperExceptionPage();
        _app.MapControllers();

        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _app!.DisposeAsync();
    }
}
