using k8s;
using k8s.Models;
using KubeOps.Operator.DependencyInjection;
using KubeOps.Operator.Entities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace KubeOps.Operator.DevOps
{
    internal class ResourceEventQueueMetrics<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private static readonly string[] Labels =
        {
            "operator",
            "kind",
            "group",
            "version",
            "scope"
        };

        public ResourceEventQueueMetrics()
        {
            var settings = DependencyInjector.Services.GetRequiredService<OperatorSettings>();
            var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TEntity>();
            var labelValues = new[]
            {
                settings.Name,
                crd.Kind,
                crd.Group,
                crd.Version,
                crd.Scope.ToString()
            };

            Running = Metrics
                .CreateGauge(
                    "operator_resource_event_queue_running",
                    "Determines if the resource event queue is up and running (1 == Running, 0 == Stopped)",
                    Labels)
                .WithLabels(labelValues);

            ReadQueueEvents = Metrics
                .CreateCounter(
                    "operator_resource_event_queue_read_events",
                    "The count of totally read events from the queue",
                    Labels)
                .WithLabels(labelValues);

            WrittenQueueEvents = Metrics
                .CreateCounter(
                    "operator_resource_event_queue_write_events",
                    "The count of totally written events from the queue",
                    Labels)
                .WithLabels(labelValues);

            QueueSizeSummary = Metrics
                .CreateSummary(
                    "operator_resource_event_queue_size",
                    "Summary of the event queue size over the last 10 minutes",
                    Labels)
                .WithLabels(labelValues);

            QueueSize = Metrics
                .CreateGauge(
                    "operator_resource_event_queue_count",
                    "Size of the entity queue",
                    Labels)
                .WithLabels(labelValues);

            CreatedEvents = Metrics
                .CreateCounter(
                    "operator_resource_event_queue_created_events",
                    "The count of total 'created' events from the queue",
                    Labels)
                .WithLabels(labelValues);

            UpdatedEvents = Metrics
                .CreateCounter(
                    "operator_resource_event_queue_updated_events",
                    "The count of total 'updated' events from the queue",
                    Labels)
                .WithLabels(labelValues);

            NotModifiedEvents = Metrics
                .CreateCounter(
                    "operator_resource_event_queue_not_modified_events",
                    "The count of total 'not modified' events from the queue",
                    Labels)
                .WithLabels(labelValues);

            StatusUpdatedEvents = Metrics
                .CreateCounter(
                    "operator_resource_event_queue_status_updated_events",
                    "The count of total 'status updated' events from the queue",
                    Labels)
                .WithLabels(labelValues);

            FinalizingEvents = Metrics
                .CreateCounter(
                    "operator_resource_event_queue_finalized_events",
                    "The count of total 'finalized' events from the queue",
                    Labels)
                .WithLabels(labelValues);

            DeletedEvents = Metrics
                .CreateCounter(
                    "operator_resource_event_queue_deleted_events",
                    "The count of total 'deleted' events from the queue",
                    Labels)
                .WithLabels(labelValues);

            RequeuedEvents = Metrics
                .CreateCounter(
                    "operator_resource_event_queue_requeued_events",
                    "The count of total events that were requeued by request of the reconciler",
                    Labels)
                .WithLabels(labelValues);

            DelayedQueueSizeSummary = Metrics
                .CreateSummary(
                    "operator_resource_event_delayed_queue_size",
                    "Summary of the delayed (requeue) event queue size over the last 10 minutes",
                    Labels)
                .WithLabels(labelValues);

            DelayedQueueSize = Metrics
                .CreateGauge(
                    "operator_resource_event_delayed_queue_count",
                    "Size of the entity delayed (requeue) queue",
                    Labels)
                .WithLabels(labelValues);

            ErroredEvents = Metrics
                .CreateCounter(
                    "operator_resource_event_queue_errored_events",
                    "The count of total events threw an error during reconciliation",
                    Labels)
                .WithLabels(labelValues);

            ErrorQueueSizeSummary = Metrics
                .CreateSummary(
                    "operator_resource_event_errored_queue_size",
                    "Summary of the error queue size over the last 10 minutes",
                    Labels)
                .WithLabels(labelValues);

            ErrorQueueSize = Metrics
                .CreateGauge(
                    "operator_resource_event_errored_queue_count",
                    "Size of the error queue",
                    Labels)
                .WithLabels(labelValues);
        }

        public Gauge.Child Running { get; }

        public Counter.Child ReadQueueEvents { get; }

        public Counter.Child WrittenQueueEvents { get; }

        public Summary.Child QueueSizeSummary { get; }

        public Gauge.Child QueueSize { get; }

        public Counter.Child CreatedEvents { get; }

        public Counter.Child UpdatedEvents { get; }

        public Counter.Child DeletedEvents { get; }

        public Counter.Child NotModifiedEvents { get; }

        public Counter.Child StatusUpdatedEvents { get; }

        public Counter.Child FinalizingEvents { get; }

        public Summary.Child DelayedQueueSizeSummary { get; }

        public Gauge.Child DelayedQueueSize { get; }

        public Counter.Child RequeuedEvents { get; }

        public Summary.Child ErrorQueueSizeSummary { get; }

        public Gauge.Child ErrorQueueSize { get; }

        public Counter.Child ErroredEvents { get; }
    }
}
