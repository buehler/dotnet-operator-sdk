using KubeOps.Operator.Webhooks.ConversionWebhook;

namespace KubeOps.Test.Integration.Operator;

public class V1ToV2TestConversionWebhook : IConversionWebhook<V1TestEntity, V2TestEntity>
{
    public V2TestEntity Convert(V1TestEntity customResourceInput)
    {
        if (customResourceInput.Spec.Spec == "throwException")
        {
            throw new ArgumentException(null, nameof(customResourceInput));
        }
        return new V2TestEntity()
        {
            ApiVersion = "integration.testing.dev/v2",
            Kind = customResourceInput.Kind,Spec = new V2TestEntitySpec()
            {
                PlaceHolder = customResourceInput.Spec.PlaceHolder, Spec = customResourceInput.Spec.Spec,
            },
            Metadata = customResourceInput.Metadata,
        };
    }
}
