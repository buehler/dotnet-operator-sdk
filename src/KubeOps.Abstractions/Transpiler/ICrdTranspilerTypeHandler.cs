using k8s.Models;

namespace KubeOps.Abstractions.Transpiler;

/// <summary>
/// Type handler for the CRD transpiler.
/// </summary>
public interface ICrdTranspilerTypeHandler
{
    /// <summary>
    /// Weather the type handler is exclusive. If an exclusive type handler
    /// is found, no other type handler will be used.
    /// </summary>
#if NETSTANDARD2_0
    bool IsExclusive { get; }
#else
    bool IsExclusive { get => false; }
#endif

    /// <summary>
    /// Weather the type handler can handle the given type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>
    /// True if the handler can handle the type of the given props,
    /// otherwise false.
    /// </returns>
    bool CanHandleType(Type type);

    /// <summary>
    /// Idempotent configuration of the given props.
    /// Configures the props for the given type.
    /// </summary>
    /// <param name="props">The props for the JSON schema that need to be configured.</param>
    /// <returns>The configured props.</returns>
    V1JSONSchemaProps Configure(V1JSONSchemaProps props);
}
