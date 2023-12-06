using KubeOps.Operator.Web.Webhooks.Admission.Mutation;

using WebhookOperator.Entities;

namespace WebhookOperator.Webhooks;

[MutationWebhook(typeof(V1TestEntity))]
public class TestMutationWebhook : MutationWebhook<V1TestEntity>
{
    public override MutationResult<V1TestEntity> Create(V1TestEntity entity, bool dryRun)
    {
        if (entity.Spec.Username == "overwrite")
        {
            entity.Spec.Username = "random overwritten";
            return Modified(entity);
        }

        return NoChanges();
    }
}
