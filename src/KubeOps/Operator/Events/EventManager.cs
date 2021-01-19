using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using k8s;
using k8s.Models;
using KubeOps.Operator.Entities.Extensions;
using Microsoft.Extensions.Logging;
using SimpleBase;

namespace KubeOps.Operator.Events
{
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

        public async Task Publish(
            IKubernetesObject<V1ObjectMeta> resource,
            string reason,
            string message,
            EventType type = EventType.Normal)
        {
            _logger.LogTrace(
                "Encoding event name with: {resourceName}.{resourceNamespace}.{reason}.{message}.{type}.",
                resource.Name(),
                resource.Namespace(),
                reason,
                message,
                type);
            var eventName =
                Base32.Rfc4648.Encode(
                    SHA512.HashData(
                        Encoding.UTF8.GetBytes($"{resource.Name()}.{resource.Namespace()}.{reason}.{message}.{type}")));
            _logger.LogTrace(@"Search or create event with name ""{name}"".", eventName);
            var @event = await _client.Get<Corev1Event>(eventName, resource.Namespace()) ??
                         new Corev1Event
                         {
                             Kind = Corev1Event.KubeKind,
                             ApiVersion = $"{Corev1Event.KubeGroup}/{Corev1Event.KubeApiVersion}",
                             Metadata = new V1ObjectMeta
                             {
                                 Name = eventName,
                                 NamespaceProperty = resource.Namespace(),
                                 Annotations = new Dictionary<string, string>
                                 {
                                     { "nameHash", "sha512" },
                                     { "nameEncoding", "Base32 / RFC 4648" },
                                 },
                             },
                             Type = type.ToString(),
                             Reason = reason,
                             Message = message,
                             ReportingComponent = _settings.Name,
                             ReportingInstance = Environment.MachineName,
                             Source = new V1EventSource { Component = _settings.Name },
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

            await _client.Save(@event);
            _logger.LogDebug(@"Created or updated event with name ""{name}"".", eventName);
        }

        public Task Publish(Corev1Event @event)
            => _client.Save(@event);

        public IEventManager.Publisher CreatePublisher(string reason, string message, EventType type = EventType.Normal)
            => resource => Publish(resource, reason, message, type);

        public IEventManager.StaticPublisher CreatePublisher(
            IKubernetesObject<V1ObjectMeta> resource,
            string reason,
            string message,
            EventType type = EventType.Normal)
            => () => Publish(resource, reason, message, type);
    }
}
