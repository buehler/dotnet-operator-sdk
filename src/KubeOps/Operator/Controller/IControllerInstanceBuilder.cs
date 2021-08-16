using System.Collections.Generic;

namespace KubeOps.Operator.Controller
{
    internal interface IControllerInstanceBuilder
    {
        public IEnumerable<IManagedResourceController> MakeManagedControllers();
    }
}
