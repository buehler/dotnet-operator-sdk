using k8s.Models;

namespace KubeOps.Operator.Entities;

/// <summary>
/// Custom CRD schema creation override to resolve https://github.com/buehler/dotnet-operator-sdk/issues/565.
/// </summary>
public class CrdBuilderResourceQuantityOverride : ICrdBuilderTypeOverride
{
    public bool HandlesType(Type type) => type.IsGenericType
                                                           && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                                                           && type.GenericTypeArguments.Contains(typeof(ResourceQuantity));

    public void ConfigureCustomSchemaForProp(V1JSONSchemaProps props)
    {
        props.Type = "object";
        props.XKubernetesPreserveUnknownFields = true;
    }
}
