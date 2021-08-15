using k8s.Models;

namespace KubeOps.Operator.Util
{
    public interface IRbacBuilder
    {
        V1ClusterRole BuildManagerRbac();
    }
}
