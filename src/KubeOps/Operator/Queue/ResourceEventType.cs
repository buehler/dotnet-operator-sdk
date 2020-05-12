namespace KubeOps.Operator.Queue
{
    public enum ResourceEventType
    {
        /// <summary>
        /// Fired when a resource (even requeued) is new to the system.
        /// </summary>
        Created,

        /// <summary>
        /// Fired when a resource (even requeued) has changed in the system.
        /// </summary>
        Updated,

        /// <summary>
        /// Fired when a resource was removed from the system.
        /// </summary>
        Deleted,

        /// <summary>
        /// Fired when a resource (even requeued) has not changed.
        /// </summary>
        NotModified,

        /// <summary>
        /// Fired when the status part of a resource changed but nothing else.
        /// </summary>
        StatusUpdated,

        /// <summary>
        /// Fired when the resource is marked for deletion but has pending finalizers.
        /// </summary>
        Finalizing,

        // TODO: events updated?
    }
}
