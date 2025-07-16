using System.Text;
using System.Text.Json.Nodes;

using Json.More;
using Json.Patch;

using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Entities;

/// <summary>
/// Method extensions for JSON diffing between two entities (<see cref="IKubernetesObject{TMetadata}"/>).
/// </summary>
public static class JsonPatchExtensions
{
    /// <summary>
    /// Convert a <see cref="IKubernetesObject{TMetadata}"/> into a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="entity">The entity to convert.</param>
    /// <returns>Either the json node, or null if it failed.</returns>
    public static JsonNode? ToNode(this IKubernetesObject<V1ObjectMeta> entity) =>
        JsonNode.Parse(KubernetesJson.Serialize(entity));

    /// <summary>
    /// Computes the JSON Patch diff between two Kubernetes entities implementing <see cref="IKubernetesObject{V1ObjectMeta}"/>.
    /// This method serializes both entities to JSON and calculates the difference as a JSON Patch document.
    /// </summary>
    /// <param name="from">The source entity to compare from.</param>
    /// <param name="to">The target entity to compare to.</param>
    /// <returns>A <see cref="JsonNode"/> representing the JSON Patch diff between the two entities.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the diff could not be created.</exception>
    public static JsonPatch CreateJsonPatch(
        this IKubernetesObject<V1ObjectMeta> from,
        IKubernetesObject<V1ObjectMeta> to)
    {
        var fromNode = from.ToNode();
        var toNode = to.ToNode();
        var patch = fromNode.CreatePatch(toNode);

        return patch;
    }

    /// <summary>
    /// Create a <see cref="V1Patch"/> out of a <see cref="JsonPatch"/>.
    /// This can be used to apply the patch to a Kubernetes entity using the Kubernetes client.
    /// </summary>
    /// <param name="patch">The patch that should be converted.</param>
    /// <returns>A <see cref="V1Patch"/> that may be applied to Kubernetes objects.</returns>
    public static V1Patch ToKubernetesPatch(this JsonPatch patch) =>
        new(patch.ToJsonString(), V1Patch.PatchType.JsonPatch);

    /// <summary>
    /// Create the unformatted JSON string representation of a <see cref="JsonPatch"/>.
    /// </summary>
    /// <param name="patch">The <see cref="JsonPatch"/> to convert.</param>
    /// <returns>A string that represents the unformatted JSON representation of the patch.</returns>
    public static string ToJsonString(this JsonPatch patch) => patch.ToJsonDocument().RootElement.GetRawText();

    /// <summary>
    /// Create the base 64 representation of a <see cref="JsonPatch"/>.
    /// </summary>
    /// <param name="patch">The patch to convert.</param>
    /// <returns>The base64 encoded representation of the patch.</returns>
    public static string ToBase64String(this JsonPatch patch) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(patch.ToJsonString()));
}
