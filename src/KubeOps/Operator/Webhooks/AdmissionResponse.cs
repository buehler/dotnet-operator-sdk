namespace KubeOps.Operator.Webhooks
{
    internal sealed class AdmissionResponse
    {
        public string Uid { get; init; } = string.Empty;

        public bool Allowed { get; init; }

        public Reason? Status { get; init; }

        public string[] Warnings { get; init; } = new string[0];

        internal sealed class Reason
        {
            public int Code { get; init; }

            public string Message { get; init; } = string.Empty;
        }
    }
}
