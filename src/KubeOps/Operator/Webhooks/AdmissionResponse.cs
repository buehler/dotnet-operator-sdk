namespace KubeOps.Operator.Webhooks;

internal sealed class AdmissionResponse
{
    public const string JsonPatch = "JSONPatch";

    public string Uid { get; set; } = string.Empty;

    public bool Allowed { get; init; }

    public Reason? Status { get; init; }

    public string[] Warnings { get; init; } = Array.Empty<string>();

    public string? PatchType { get; set; }

    public string? Patch { get; set; }

    internal sealed class Reason
    {
        public int Code { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
