using k8s.Models;

namespace KubeOps.Operator.Rbac
{
    public interface IRbacBuilder
    {
        V1ClusterRole BuildManagerRbac();
    }
}
