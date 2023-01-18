using System.Security.Cryptography;
using System.Text;
using k8s;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Entities.Extensions;
using SimpleBase;

namespace KubeOps.Operator.Events;

internal class EventManager : IEventManager
{
    private readonly IKubernetesClient _client;
    private readonly OperatorSettings _settings;
    private readonly ILogger<EventManager> _logger;

    public EventManager(IKubernetesClient client, OperatorSettings settings, ILogger<EventManager> logger)
    {
        _client = client;
        _settings = settings;
        _logger = logger;
    }

    public async Task PublishAsync(
        IKubernetesObject<V1ObjectMeta> resource,
        string reason,
        string message,
        EventType type = EventType.Normal)
    {
        var resourceNamespace = resource.Namespace() ?? "default";

        _logger.LogTrace(
            "Encoding event name with: {resourceName}.{resourceNamespace}.{reason}.{message}.{type}.",
            resource.Name(),
            resourceNamespace,
            reason,
            message,
            type);
        var eventName =
            Base32.Rfc4648.Encode(
                SHA512.HashData(
                    Encoding.UTF8.GetBytes($"{resource.Name()}.{resourceNamespace}.{reason}.{message}.{type}")));
        _logger.LogTrace(@"Search or create event with name ""{name}"".", eventName);
        var @event = await _client.Get<Corev1Event>(eventName, resourceNamespace) ??
                     new Corev1Event
                     {
                         Kind = Corev1Event.KubeKind,
                         ApiVersion = $"{Corev1Event.KubeGroup}/{Corev1Event.KubeApiVersion}",
                         Metadata = new()
                         {
                             Name = eventName,
                             NamespaceProperty = resourceNamespace,
                             Annotations = new Dictionary<string, string>
                             {
                                 { "nameHash", "sha512" }, { "nameEncoding", "Base32 / RFC 4648" },
                             },
                         },
                         Type = type.ToString(),
                         Reason = reason,
                         Message = message,
                         ReportingComponent = _settings.Name,
                         ReportingInstance = Environment.MachineName,
                         Source = new() { Component = _settings.Name, },
                         InvolvedObject = resource.MakeObjectReference(),
                         FirstTimestamp = DateTime.UtcNow,
                         LastTimestamp = DateTime.UtcNow,
                         Count = 0,
                     };

        @event.Count++;
        @event.LastTimestamp = DateTime.UtcNow;
        _logger.LogTrace(
            "Save event with new count {count} and last timestamp {timestamp}",
            @event.Count,
            @event.LastTimestamp);

        try
        {
            await _client.Save(@event);
            _logger.LogInformation(
                @"Created or updated event with name ""{name}"" to new count {count} on resource ""{kind}/{name}"".",
                eventName,
                @event.Count,
                resource.Kind,
                resource.Name());
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                @"Could not publish event with name ""{name}"" on resource ""{kind}/{name}"".",
                eventName,
                resource.Kind,
                resource.Name());
        }
    }

    public Task PublishAsync(Corev1Event @event)
        => _client.Save(@event);

    public IEventManager.AsyncPublisher CreatePublisher(
        string reason,
        string message,
        EventType type = EventType.Normal)
        => resource => PublishAsync(resource, reason, message, type);

    public IEventManager.AsyncStaticPublisher CreatePublisher(
        IKubernetesObject<V1ObjectMeta> resource,
        string reason,
        string message,
        EventType type = EventType.Normal)
        => () => PublishAsync(resource, reason, message, type);

    public IEventManager.AsyncMessagePublisher CreatePublisher(string reason, EventType type = EventType.Normal)
        => (resource, message) => PublishAsync(resource, reason, message, type);
}
