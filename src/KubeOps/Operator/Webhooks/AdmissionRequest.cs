namespace KubeOps.Operator.Webhooks
{
    internal sealed class AdmissionRequest<TEntity>
    {
        public string Uid { get; init; } = string.Empty;

        public string Operation { get; init; } = string.Empty;

        public TEntity? Object { get; set; }

        public TEntity? OldObject { get; set; }

        public bool DryRun { get; set; }
    }
}
