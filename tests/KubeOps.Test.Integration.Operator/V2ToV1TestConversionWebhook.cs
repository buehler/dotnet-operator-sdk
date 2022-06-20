using KubeOps.Operator.Webhooks.ConversionWebhook;

namespace KubeOps.Test.Integration.Operator;

public class V2ToV1TestConversionWebhook : IConversionWebhook<V2TestEntity, V1TestEntity>
{
    public V1TestEntity Convert(V2TestEntity customResourceInput)
    {
        if (customResourceInput.Spec.Spec == "throwException")
        {
            throw new ArgumentException(null, nameof(customResourceInput));
        }
        return new V1TestEntity()
        {
            ApiVersion = "integration.testing.dev/v1",
            Kind = customResourceInput.Kind,Spec = new V1TestEntitySpec()
            {
                PlaceHolder = customResourceInput.Spec.PlaceHolder, Spec = customResourceInput.Spec.Spec,
            },
            Metadata = customResourceInput.Metadata,
        };
    }
}
