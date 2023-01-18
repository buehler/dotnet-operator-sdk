using KubeOps.Operator.Webhooks;
using KubeOps.TestOperator.Entities;

namespace KubeOps.TestOperator.Webhooks;

public class TestMutator : IMutationWebhook<V2TestEntity>
{
    public AdmissionOperations Operations => AdmissionOperations.Create;

    public MutationResult Create(V2TestEntity newEntity, bool dryRun)
    {
        newEntity.Spec.StringOrInteger = "42";
        return MutationResult.Modified(newEntity);
    }
}
