using k8s.Models;

namespace KubeOps.Operator.Webhooks
{
    public record WebhookConfig(
        string OperatorName,
        string? BaseUrl,
        byte[]? CaBundle,
        Admissionregistrationv1ServiceReference? Service);
}
