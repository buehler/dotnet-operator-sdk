using k8s.Models;

namespace KubeOps.Operator.Entities;

internal interface ICrdBuilder
{
    IEnumerable<V1CustomResourceDefinition> BuildCrds();
}
