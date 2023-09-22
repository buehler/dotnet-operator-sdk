using k8s.Models;

namespace KubeOps.Operator.Rbac;

internal interface IRbacBuilder
{
    V1ClusterRole BuildManagerRbac();
}
