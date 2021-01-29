using DotnetKubernetesClient.Entities;
using k8s;
using k8s.Models;
using Prometheus;

namespace KubeOps.Operator.DevOps
{
    internal class ResourceControllerMetrics<TEntity>
        where TEntity : IKubernetesObject<V1ObjectMeta>
    {
        private static readonly string[] Labels =
        {
            "operator",
            "kind",
            "group",
            "version",
            "scope",
        };

        public ResourceControllerMetrics(OperatorSettings settings)
        {
            var crd = CustomEntityDefinitionExtensions.CreateResourceDefinition<TEntity>();
            var labelValues = new[]
            {
                settings.Name,
                crd.Kind,
                crd.Group,
                crd.Version,
                crd.Scope.ToString(),
            };

            Running = Metrics
                .CreateGauge(
                    "operator_resource_controller_running",
                    "Determines if the resource watcher is up and running (1 == Running, 0 == Stopped)",
                    Labels)
                .WithLabels(labelValues);

            EventsFromWatcher = Metrics
                .CreateCounter(
                    "operator_resource_controller_watched_events",
                    "The count of totally watched events from the resource watcher",
                    Labels)
                .WithLabels(labelValues);

            RequeuedEvents = Metrics
                .CreateCounter(
                    "operator_resource_controller_requeued_events",
                    "The count of totally requeued resources (normal requeue, without errors)",
                    Labels)
                .WithLabels(labelValues);

            ErroredEvents = Metrics
                .CreateCounter(
                    "operator_resource_controller_errored_events",
                    "The count of totally errored reconciliation attempts.",
                    Labels)
                .WithLabels(labelValues);

            CreatedEvents = Metrics
                .CreateCounter(
                    "operator_resource_controller_created_events",
                    "The count of total 'created' events reconciled by the controller",
                    Labels)
                .WithLabels(labelValues);

            UpdatedEvents = Metrics
                .CreateCounter(
                    "operator_resource_controller_updated_events",
                    "The count of total 'updated' events reconciled by the controller",
                    Labels)
                .WithLabels(labelValues);

            NotModifiedEvents = Metrics
                .CreateCounter(
                    "operator_resource_controller_not_modified_events",
                    "The count of total 'not modified' events reconciled by the controller",
                    Labels)
                .WithLabels(labelValues);

            StatusUpdatedEvents = Metrics
                .CreateCounter(
                    "operator_resource_controller_status_updated_events",
                    "The count of total 'status updated' events reconciled by the controller",
                    Labels)
                .WithLabels(labelValues);

            FinalizingEvents = Metrics
                .CreateCounter(
                    "operator_resource_controller_finalized_events",
                    "The count of total 'finalized' events reconciled by the controller",
                    Labels)
                .WithLabels(labelValues);

            DeletedEvents = Metrics
                .CreateCounter(
                    "operator_resource_controller_deleted_events",
                    "The count of total 'deleted' events reconciled by the controller",
                    Labels)
                .WithLabels(labelValues);
        }

        public Gauge.Child Running { get; }

        public Counter.Child EventsFromWatcher { get; }

        public Counter.Child RequeuedEvents { get; }

        public Counter.Child ErroredEvents { get; }

        public Counter.Child CreatedEvents { get; }

        public Counter.Child UpdatedEvents { get; }

        public Counter.Child DeletedEvents { get; }

        public Counter.Child NotModifiedEvents { get; }

        public Counter.Child StatusUpdatedEvents { get; }

        public Counter.Child FinalizingEvents { get; }
    }
}
