using DotnetKubernetesClient.Entities;

namespace KubeOps.Operator.Entities.Extensions;

public static class CustomEntityDefinitionExtensionMethods
{
    public static string GroupVersion(this CustomEntityDefinition ced)
    {
        return $"{ced.Group}/{ced.Version}";
    }
}
