using System.Collections.Generic;
using k8s.Models;

namespace KubeOps.Operator.Entities
{
    public interface ICrdBuilder
    {
        IEnumerable<V1CustomResourceDefinition> BuildCrds();
    }
}
