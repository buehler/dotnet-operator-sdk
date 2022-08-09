using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using k8s;
using k8s.Models;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.DevOps;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Kubernetes;
using static KubeOps.Operator.Builder.IComponentRegistrar;

namespace KubeOps.Operator.Controller;

internal class ManagedResourceController<TEntity> : IManagedResourceController
    where TEntity : class, IKubernetesObject<V1ObjectMeta>
{
    private readonly ILogger<ManagedResourceController<TEntity>> _logger;
    private readonly IServiceProvider _services;
    private readonly ResourceControllerMetrics<TEntity> _metrics;
    private readonly OperatorSettings _settings;
    private readonly ControllerRegistration _controllerRegistration;
    private readonly IEventQueue<TEntity> _eventQueue;

    private IDisposable? _eventSubscription;

    public ManagedResourceController(
        ILogger<ManagedResourceController<TEntity>> logger,
        IServiceProvider services,
        ResourceControllerMetrics<TEntity> metrics,
        OperatorSettings settings,
        ControllerRegistration controllerRegistration,
        IEventQueue<TEntity> eventQueue)
    {
        _logger = logger;
        _services = services;
        _metrics = metrics;
        _settings = settings;
        _controllerRegistration = controllerRegistration;
        _eventQueue = eventQueue;

        Events = _eventQueue
            .Events
            .Select(HandleEvent)
            .Concat();
    }

    private IObservable<Unit> Events { get; }

    public virtual async Task StartAsync()
    {
        _logger.LogDebug(@"Managed resource controller startup for type ""{type}"".", typeof(TEntity));
        _eventSubscription = Events.Subscribe();

        await _eventQueue.StartAsync(_ => _metrics.EventsFromWatcher.Inc());
        _metrics.Running.Set(1);
    }

    public virtual async Task StopAsync()
    {
        _logger.LogTrace(@"Managed resource controller shutdown for type ""{type}"".", typeof(TEntity));

        await _eventQueue.StopAsync();
        _eventSubscription?.Dispose();
        _eventSubscription = null;
    }

    public void Dispose()
    {
        _logger.LogTrace(@"Managed resource controller disposal for type ""{type}"".", typeof(TEntity));
        _eventSubscription?.Dispose();
        _eventSubscription = null;
        _metrics.Running.Set(0);
    }

    protected async Task HandleResourceEvent(ResourceEvent<TEntity> resourceEvent)
    {
        (ResourceEventType eventType, TEntity resource, _, _) = resourceEvent;

        _logger.LogDebug(
            @"Execute/Reconcile event ""{eventType}"" on resource ""{kind}/{name}"".",
            eventType,
            resource.Kind,
            resource.Name());

        var controllerType = _controllerRegistration.ControllerType;

        ResourceControllerResult? result = null;
        _logger.LogTrace(@"Instantiating new DI scope for controller ""{name}"".", controllerType.Name);
        using (var scope = _services.CreateScope())
        {
            if (scope.ServiceProvider.GetRequiredService(controllerType) is not IResourceController<TEntity>
                controller)
            {
                var ex = new InvalidCastException(
                    $@"The type ""{controllerType.Namespace}.{controllerType.Name}"" is not a valid IResourceController<TEntity> type.");
                _logger.LogCritical(
                    @"The type ""{namespace}.{name}"" is not a valid IResourceController<TEntity> type.",
                    controllerType.Namespace,
                    controllerType.Name);
                throw ex;
            }

            try
            {
                switch (eventType)
                {
                    case ResourceEventType.Reconcile:
                        result = await controller.ReconcileAsync(resource);
                        _metrics.ReconciledEvents.Inc();
                        break;
                    case ResourceEventType.Deleted:
                        await controller.DeletedAsync(resource);
                        _metrics.DeletedEvents.Inc();
                        _logger.LogInformation(
                            @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue not possible.",
                            eventType,
                            resource.Kind,
                            resource.Name());
                        return;
                    case ResourceEventType.StatusUpdated:
                        await controller.StatusModifiedAsync(resource);
                        _metrics.StatusUpdatedEvents.Inc();
                        _logger.LogInformation(
                            @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue not possible.",
                            eventType,
                            resource.Kind,
                            resource.Name());
                        return;
                }
            }
            catch (Exception e)
            {
                RequeueError(resourceEvent, e);
                return;
            }
        }

        switch (result)
        {
            case null:
                _logger.LogInformation(
                    @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue not requested.",
                    eventType,
                    resource.Kind,
                    resource.Name());
                return;
            case RequeueEventResult requeue:
                var specificQueueTypeRequested = requeue.EventType.HasValue;
                var requestedQueueType = requeue.EventType ??
                                         (_settings.DefaultRequeueAsSameType
                                             ? eventType
                                             : ResourceEventType.Reconcile);

                if (specificQueueTypeRequested)
                {
                    _logger.LogInformation(
                        @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue requested as type ""{requeueType}"" with delay ""{requeue}"".",
                        eventType,
                        resource.Kind,
                        resource.Name(),
                        requestedQueueType,
                        requeue.RequeueIn);
                }
                else
                {
                    _logger.LogInformation(
                        @"Event type ""{eventType}"" on resource ""{kind}/{name}"" successfully reconciled. Requeue requested with delay ""{requeue}"".",
                        eventType,
                        resource.Kind,
                        resource.Name(),
                        requeue.RequeueIn);
                }

                RequeueDelayed(resourceEvent with { Type = requestedQueueType }, requeue.RequeueIn);
                break;
        }
    }

    protected async Task HandleResourceFinalization(ResourceEvent<TEntity>? resourceEvent)
    {
        using var scope = _services.CreateScope();

        if (resourceEvent == null)
        {
            return;
        }

        (_, TEntity resource, _, _) = resourceEvent;

        _logger.LogDebug(
            @"Finalize resource ""{kind}/{name}"".",
            resource.Kind,
            resource.Name());

        try
        {
            await scope.ServiceProvider.GetRequiredService<IFinalizerManager<TEntity>>()
                .FinalizeAsync(resourceEvent.Resource);
        }
        catch (Exception e)
        {
            RequeueError(resourceEvent, e);
        }
    }

    protected void RequeueError(ResourceEvent<TEntity> resourceEvent, Exception ex)
    {
        _metrics.ErroredEvents.Inc();

        var attempt = resourceEvent.Attempt + 1;
        var delay = _settings.ErrorBackoffStrategy(attempt);

        _logger.LogError(
            ex,
            @"Event type ""{eventType}"" on resource ""{kind}/{name}"" threw an error. Retry attempt {retryAttempt}.",
            resourceEvent.Type,
            resourceEvent.Resource.Kind,
            resourceEvent.Resource.Name(),
            attempt);

        _eventQueue.EnqueueLocal(resourceEvent with { Attempt = attempt, Delay = delay });
    }

    protected void RequeueDelayed(ResourceEvent<TEntity> resourceEvent, TimeSpan delay)
    {
        _metrics.RequeuedEvents.Inc();

        _eventQueue.EnqueueLocal(resourceEvent with { Delay = delay });
    }

    private IObservable<Unit> HandleEvent(ResourceEvent<TEntity> resourceEvent)
    {
        return Observable.FromAsync(
            async () => await (resourceEvent.Type == ResourceEventType.Finalizing
                ? HandleResourceFinalization(resourceEvent)
                : HandleResourceEvent(resourceEvent)),
            ThreadPoolScheduler.Instance);
    }
}
