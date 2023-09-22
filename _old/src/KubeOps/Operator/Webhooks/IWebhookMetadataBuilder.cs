namespace KubeOps.Operator.Webhooks;

internal interface IWebhookMetadataBuilder
{
    (string Name, string Endpoint) GetMetadata<TResult>(object hook, Type resourceType)
        where TResult : AdmissionResult;
}
