using KubeOps.Operator.Web.Webhooks.Admission.Mutation;

using GeneratedOperatorProject.Entities;

namespace GeneratedOperatorProject.Webhooks;

[MutationWebhook(typeof(V1DemoEntity))]
public class TestMutationWebhook : MutationWebhook<V1DemoEntity>
{
    public override MutationResult<V1DemoEntity> Create(V1DemoEntity entity, bool dryRun)
    {
        if (entity.Spec.Username == "overwrite")
        {
            entity.Spec.Username = "random overwritten";
            return Modified(entity);
        }

        return NoChanges();
    }
}
