using k8s.Models;

namespace KubeOps.Operator.Webhooks;

/// <summary>
/// Implement this interface to configure the ValidatingWebhook object before it is applied to Kubernetes.
/// </summary>
public interface IConfigurableValidationWebhook
{
    /// <summary>
    /// Called when building the ValidatingWebhook to allow further customizations before it is applied.
    /// </summary>
    /// <param name="webhook">The ValidatingWebhook that will be applied.</param>
    void Configure(V1ValidatingWebhook webhook);
}
