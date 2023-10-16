using KubeOps.Operator.Web.Webhooks.Mutation;

namespace KubeOps.Operator.Web.Test.TestApp;

[MutationWebhook(typeof(V1IntegrationTestEntity))]
public class TestMutationWebhook : MutationWebhook<V1IntegrationTestEntity>
{
    public override MutationResult<V1IntegrationTestEntity> Create(V1IntegrationTestEntity entity, bool dryRun)
    {
        if (entity.Spec.Username == "overwrite")
        {
            entity.Spec.Username = "overwritten";
            return Modified(entity);
        }

        return NoChanges();
    }

    public override MutationResult<V1IntegrationTestEntity> Update(
        V1IntegrationTestEntity oldEntity,
        V1IntegrationTestEntity newEntity,
        bool dryRun)
    {
        if (newEntity.Spec.Username == "overwrite")
        {
            newEntity.Spec.Username = "overwritten";
            return Modified(newEntity);
        }

        return NoChanges();
    }
}
