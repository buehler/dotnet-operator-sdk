using System.Reflection;

namespace KubeOps.Transpiler;

public static class ContextCreator
{
    public static MetadataLoadContext Create(
        IEnumerable<string> assemblyPaths,
        byte[] assembly,
        string? coreAssemblyName = null)
    {
        var mlc = Create(assemblyPaths, coreAssemblyName);
        mlc.LoadFromByteArray(assembly);
        return mlc;
    }

    public static MetadataLoadContext Create(IEnumerable<string> assemblyPaths, string? coreAssemblyName = null) =>
        new(new PathAssemblyResolver(assemblyPaths), coreAssemblyName: coreAssemblyName);
}
