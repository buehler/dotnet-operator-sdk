using System.Collections.Generic;
using k8s.Models;

namespace KubeOps.Operator.Util
{
    public interface ICrdBuilder
    {
        IEnumerable<V1CustomResourceDefinition> BuildCrds();
    }
}
