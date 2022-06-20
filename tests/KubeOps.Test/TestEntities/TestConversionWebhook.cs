using KubeOps.Operator.Webhooks.ConversionWebhook;

namespace KubeOps.Test.TestEntities;

public class TestConversionWebhook : IConversionWebhook<ConversionTestEntityV1Beta, ConversionTestEntityV1>
{
    public ConversionTestEntityV1 Convert(ConversionTestEntityV1Beta customResourceInput)
    {
        return new ConversionTestEntityV1();
    }
}
