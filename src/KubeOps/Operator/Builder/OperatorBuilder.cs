using System.Reflection;
using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Caching;
using KubeOps.Operator.Controller;
using KubeOps.Operator.DevOps;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Events;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Kubernetes;
using KubeOps.Operator.Leadership;
using KubeOps.Operator.Rbac;
using KubeOps.Operator.Webhooks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

namespace KubeOps.Operator.Builder;

internal class OperatorBuilder : IOperatorBuilder
{
    internal const string LivenessTag = "liveness";
    internal const string ReadinessTag = "readiness";
    private readonly IComponentRegistrar _componentRegistrar;
    private IAssemblyScanner _assemblyScanner;

    public OperatorBuilder(IServiceCollection services)
    {
        Services = services;
        _assemblyScanner = new AssemblyScanner(this);
        _componentRegistrar = new ComponentRegistrar();
    }

    public IServiceCollection Services { get; }

    public IOperatorBuilder AddHealthCheck<THealthCheck>(string? name = default)
        where THealthCheck : class, IHealthCheck
    {
        Services
            .AddHealthChecks()
            .AddCheck<THealthCheck>(
                name ?? typeof(THealthCheck).Name,
                tags: new[] { ReadinessTag, LivenessTag });

        return this;
    }

    public IOperatorBuilder AddReadinessCheck<TReadinessCheck>(string? name = default)
        where TReadinessCheck : class, IHealthCheck
    {
        Services
            .AddHealthChecks()
            .AddCheck<TReadinessCheck>(
                name ?? typeof(TReadinessCheck).Name,
                tags: new[] { ReadinessTag });

        return this;
    }

    public IOperatorBuilder AddLivenessCheck<TLivenessCheck>(string? name = default)
        where TLivenessCheck : class, IHealthCheck
    {
        Services
            .AddHealthChecks()
            .AddCheck<TLivenessCheck>(
                name ?? typeof(TLivenessCheck).Name,
                tags: new[] { LivenessTag });

        return this;
    }

    public IOperatorBuilder AddResourceAssembly(Assembly assembly)
    {
        _assemblyScanner.AddAssembly(assembly);

        return this;
    }

    public IOperatorBuilder AddEntity<TEntity>()
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        _componentRegistrar.RegisterEntity<TEntity>();

        return this;
    }

    public IOperatorBuilder AddController<TImplementation, TEntity>()
        where TImplementation : class, IResourceController<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        Services.TryAddScoped<TImplementation>();

        _componentRegistrar.RegisterController<TImplementation, TEntity>();

        return this;
    }

    public IOperatorBuilder AddFinalizer<TImplementation, TEntity>()
        where TImplementation : class, IResourceFinalizer<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        Services.TryAddScoped<TImplementation>();
        _componentRegistrar.RegisterFinalizer<TImplementation, TEntity>();

        return this;
    }

    public IOperatorBuilder AddValidationWebhook<TImplementation, TEntity>()
        where TImplementation : class, IValidationWebhook<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        Services.TryAddScoped<TImplementation>();
        _componentRegistrar.RegisterValidator<TImplementation, TEntity>();

        return this;
    }

    public IOperatorBuilder AddMutationWebhook<TImplementation, TEntity>()
        where TImplementation : class, IMutationWebhook<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        Services.TryAddScoped<TImplementation>();
        _componentRegistrar.RegisterMutator<TImplementation, TEntity>();

        return this;
    }

    public IOperatorBuilder AddWebhookLocaltunnel(
        string hostname = "localhost",
        short port = 5000,
        bool isHttps = false,
        bool allowUntrustedCertificates = true)
    {
        Services.AddHostedService(
            services => new WebhookLocalTunnel(
                services.GetRequiredService<ILogger<WebhookLocalTunnel>>(),
                services.GetRequiredService<OperatorSettings>(),
                services.GetRequiredService<IKubernetesClient>(),
                services.GetRequiredService<MutatingWebhookConfigurationBuilder>(),
                services.GetRequiredService<ValidatingWebhookConfigurationBuilder>())
            {
                Host = hostname,
                Port = port,
                IsHttps = isHttps,
                AllowUntrustedCertificates = allowUntrustedCertificates,
            });

        return this;
    }

    internal IOperatorBuilder AddOperatorBase(OperatorSettings settings)
    {
        if (settings.EnableAssemblyScanning)
        {
            _assemblyScanner
                .AddAssembly(Assembly.GetEntryAssembly() ?? throw new Exception("No Entry Assembly found."))
                .AddAssembly(Assembly.GetExecutingAssembly());
        }
        else
        {
            // This will cause calls to IOperatorBuilder.AddResourceAssembly to throw an InvalidOperationException
            _assemblyScanner = new DisabledAssemblyScanner();
        }

        Services.AddSingleton(settings);

        Services.AddSingleton(_ => _componentRegistrar);

        Services.AddSingleton<IControllerInstanceBuilder, ControllerInstanceBuilder>();
        Services.AddSingleton<IControllerInstanceBuilder.ControllerFactory>(
            (parent, registration) =>
            {
                var childScope = parent.CreateScope();
                var controller = (IManagedResourceController)ActivatorUtilities.CreateInstance(
                    childScope.ServiceProvider,
                    typeof(ManagedResourceController<>).MakeGenericType(registration.EntityType),
                    registration);

                return new ScopedResourceController(childScope, controller);
            });

        Services.AddTransient<IFinalizerInstanceBuilder, FinalizerInstanceBuilder>();

        Services.AddTransient<MutatingWebhookBuilder>();
        Services.AddTransient<MutatingWebhookConfigurationBuilder>();
        Services.AddTransient<ValidatingWebhookBuilder>();
        Services.AddTransient<ValidatingWebhookConfigurationBuilder>();
        Services.AddTransient<IWebhookMetadataBuilder, WebhookMetadataBuilder>();

        Services.AddSingleton<IKubernetesClient, KubernetesClient.KubernetesClient>();
        Services.AddScoped<IEventManager, EventManager>();

        Services.AddScoped(typeof(IResourceCache<>), typeof(ResourceCache<>));
        Services.AddScoped(typeof(IResourceWatcher<>), typeof(ResourceWatcher<>));
        Services.AddScoped(typeof(IEventQueue<>), typeof(EventQueue<>));

        // Support all the metrics
        Services.AddSingleton(typeof(ResourceWatcherMetrics<>));
        Services.AddSingleton(typeof(ResourceCacheMetrics<>));
        Services.AddSingleton(typeof(ResourceControllerMetrics<>));

        // Support for healthchecks and prometheus.
        Services
            .AddHealthChecks()
            .ForwardToPrometheus();

        // Support for leader election via V1Leases.
        if (settings.EnableLeaderElection)
        {
            Services.AddHostedService<LeaderElector>();
            Services.AddSingleton<ILeaderElection, LeaderElection>();
        }
        else
        {
            Services.AddSingleton<ILeaderElection, DisabledLeaderElection>();
        }

        // Register event handler
        Services.AddTransient<IEventManager, EventManager>();

        // Register controller manager
        Services.AddHostedService<ResourceControllerManager>();

        // Register finalizer manager
        Services.AddTransient(typeof(IFinalizerManager<>), typeof(FinalizerManager<>));

        // Register builders for RBAC rules and CRDs
        Services.TryAddSingleton<ICrdBuilder, CrdBuilder>();
        Services.TryAddSingleton<IRbacBuilder, RbacBuilder>();

        return this;
    }
}
