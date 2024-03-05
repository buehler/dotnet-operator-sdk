using System.Runtime.CompilerServices;

using k8s;
using k8s.Models;

namespace KubeOps.Operator.Test;

public class InvocationCounter<TEntity>
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private TaskCompletionSource _task = new();
    public readonly List<(string Method, TEntity Entity)> Invocations = [];

    public Task WaitForInvocations => _task.Task;

    public int TargetInvocationCount { get; set; } = 1;

    public void Invocation(TEntity entity, [CallerMemberName] string name = "Invocation")
    {
        Invocations.Add((name, entity));
        if (Invocations.Count >= TargetInvocationCount)
        {
            _task.TrySetResult();
        }
    }

    public void Clear()
    {
        Invocations.Clear();
        _task = new TaskCompletionSource();
        TargetInvocationCount = 1;
    }
}
