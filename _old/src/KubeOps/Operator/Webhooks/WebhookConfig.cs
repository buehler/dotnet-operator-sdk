using k8s.Models;

namespace KubeOps.Operator.Webhooks;

internal record WebhookConfig(
    string OperatorName,
    string? BaseUrl,
    byte[]? CaBundle,
    Admissionregistrationv1ServiceReference? Service);
