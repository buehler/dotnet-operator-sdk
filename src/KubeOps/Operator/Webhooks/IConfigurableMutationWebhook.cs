using k8s.Models;

namespace KubeOps.Operator.Webhooks;

/// <summary>
/// Implement this interface to configure the MutatingWebhook object before it is applied to Kubernetes.
/// </summary>
public interface IConfigurableMutationWebhook
{
    /// <summary>
    /// Called when building the MutatingWebhook to allow further customizations before it is applied.
    /// </summary>
    /// <param name="webhook">The MutatingWebhook that will be applied.</param>
    void Configure(V1MutatingWebhook webhook);
}
