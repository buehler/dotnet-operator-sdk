using System;

namespace KubeOps.Operator.Controller.Results
{
    internal sealed class RequeueEventResult : ResourceControllerResult
    {
        public RequeueEventResult(TimeSpan requeueIn)
            : base(requeueIn)
        {
        }
    }
}
