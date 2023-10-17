using k8s.Models;

using KubeOps.Operator.Web.Webhooks.Conversion;

using WebhookOperator.Entities;

namespace WebhookOperator.Webhooks;

[ConversionWebhook(typeof(V2TestEntity))]
public class TestConversionWebhook : ConversionWebhook
{
    public TestConversionWebhook()
    {
        RegisterConverter<V1TestEntity, V2TestEntity>(From1To2);
        RegisterConverter<V2TestEntity, V1TestEntity>(From2To1);
    }

    private static V2TestEntity From1To2(V1TestEntity entity)
    {
        var nameSplit = entity.Spec.Name.Split(' ');
        var result = new V2TestEntity { Metadata = entity.Metadata };
        result.Spec.Firstname = nameSplit[0];
        result.Spec.Lastname = string.Join(' ', nameSplit[1..]);
        return result;
    }

    private static V1TestEntity From2To1(V2TestEntity entity)
    {
        var result = new V1TestEntity { Metadata = entity.Metadata };
        result.Spec.Name = $"{entity.Spec.Firstname} {entity.Spec.Lastname}";
        return result;
    }
}
