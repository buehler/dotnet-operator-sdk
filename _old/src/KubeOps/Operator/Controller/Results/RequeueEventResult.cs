using KubeOps.Operator.Kubernetes;

namespace KubeOps.Operator.Controller.Results;

internal sealed class RequeueEventResult : ResourceControllerResult
{
    public RequeueEventResult(TimeSpan requeueIn)
        : base(requeueIn)
    {
    }

    public RequeueEventResult(TimeSpan requeueIn, ResourceEventType eventType)
        : base(requeueIn, eventType)
    {
    }
}
