using System.Reflection;

namespace KubeOps.Operator.Builder;

internal class DisabledAssemblyScanner : IAssemblyScanner
{
    public IAssemblyScanner AddAssembly(Assembly assembly)
    {
        throw new InvalidOperationException("Assembly scanning is disabled by current operator configuration.");
    }
}
