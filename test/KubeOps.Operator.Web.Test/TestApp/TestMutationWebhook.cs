using KubeOps.Operator.Web.Webhooks.Mutation;

namespace KubeOps.Operator.Web.Test.TestApp;

[MutationWebhook(typeof(V1OperatorWebIntegrationTestEntity))]
public class TestMutationWebhook : MutationWebhook<V1OperatorWebIntegrationTestEntity>
{
    public override MutationResult<V1OperatorWebIntegrationTestEntity> Create(V1OperatorWebIntegrationTestEntity entity,
        bool dryRun)
    {
        if (entity.Spec.Username == "overwrite")
        {
            entity.Spec.Username = "overwritten";
            return Modified(entity);
        }

        return NoChanges();
    }
}
