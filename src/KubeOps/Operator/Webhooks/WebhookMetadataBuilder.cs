using System.Reflection;

namespace KubeOps.Operator.Webhooks;

internal class WebhookMetadataBuilder : IWebhookMetadataBuilder
{
    public (string Name, string Endpoint) GetMetadata<TResult>(object hook, Type resourceType)
        where TResult : AdmissionResult
    {
        var nameProperty = typeof(IAdmissionWebhook<,>)
            .MakeGenericType(resourceType, typeof(TResult))
            .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
            .First(p => p.Name == "Name");
        var endpointProperty = typeof(IAdmissionWebhook<,>)
            .MakeGenericType(resourceType, typeof(TResult))
            .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
            .First(p => p.Name == "Endpoint");

        return (
            nameProperty.GetValue(hook)?.ToString() ?? string.Empty,
            endpointProperty.GetValue(hook)?.ToString() ?? string.Empty);
    }
}
