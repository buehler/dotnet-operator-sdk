using System.Reflection;

namespace KubeOps.Operator.Builder;

internal interface IAssemblyScanner
{
    public IAssemblyScanner AddAssembly(Assembly assembly);
}
