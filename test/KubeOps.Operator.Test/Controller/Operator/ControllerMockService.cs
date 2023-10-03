using KubeOps.Operator.Test.TestEntities;

namespace KubeOps.Operator.Test.Controller.Operator;

public class ControllerMockService
{
    private TaskCompletionSource _task = new();
    public readonly List<(string Method, V1IntegrationTestEntity Entity)> Invocations = new();

    public Task WaitForInvocations => _task.Task;

    public int TargetInvocationCount { get; set; } = 1;

    public void Reconcile(V1IntegrationTestEntity entity)
    {
        Invocations.Add(("Reconcile", entity));
        if (Invocations.Count >= TargetInvocationCount)
        {
            _task.SetResult();
        }
    }

    public void Delete(V1IntegrationTestEntity entity)
    {
        Invocations.Add(("Delete", entity));
        if (Invocations.Count >= TargetInvocationCount)
        {
            _task.SetResult();
        }
    }

    public void Clear()
    {
        Invocations.Clear();
        _task = new TaskCompletionSource();
        TargetInvocationCount = 1;
    }
}
