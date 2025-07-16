using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
using System.Text.Json.Nodes;

using k8s;
using k8s.Models;

namespace KubeOps.Abstractions.Entities;

/// <summary>
/// Method extensions for JSON diffing between two entities (<see cref="IKubernetesObject{TMetadata}"/>).
/// </summary>
public static class JsonDiffExtensions
{
    private static readonly JsonPatchDeltaFormatter Formatter = new();

    private static readonly Lazy<JsonSerializerOptions> Options = new(() =>
    {
        JsonSerializerOptions options = null!;
        KubernetesJson.AddJsonOptions(c => options = c);
        return options;
    });

    /// <summary>
    /// Computes the JSON Patch diff between two Kubernetes entities implementing <see cref="IKubernetesObject{V1ObjectMeta}"/>.
    /// This method serializes both entities to JSON and calculates the difference as a JSON Patch document.
    /// </summary>
    /// <param name="from">The source entity to compare from.</param>
    /// <param name="to">The target entity to compare to.</param>
    /// <param name="options">Optional diffing options to control the diffing behavior.</param>
    /// <returns>A <see cref="JsonNode"/> representing the JSON Patch diff between the two entities.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the diff could not be created.</exception>
    public static JsonNode GetJsonDiff(
        this IKubernetesObject<V1ObjectMeta> from,
        IKubernetesObject<V1ObjectMeta> to,
        JsonDiffOptions? options = null)
    {
        var fromNode = JsonSerializer.SerializeToNode(from, Options.Value);
        var toNode = JsonSerializer.SerializeToNode(to, Options.Value);

        var patch = fromNode.Diff(toNode, Formatter, options) ??
                    throw new InvalidOperationException("Failed to create JSON diff.");

        return patch;
    }

    /// <summary>
    /// Computes the JSON Patch diff between two Kubernetes entities and returns it as a <see cref="V1Patch"/> object.
    /// This is useful for applying the patch directly to Kubernetes resources.
    /// </summary>
    /// <param name="from">The source entity to compare from.</param>
    /// <param name="to">The target entity to compare to.</param>
    /// <param name="options">Optional diffing options to control the diffing behavior.</param>
    /// <returns>A <see cref="V1Patch"/> object containing the JSON Patch diff.</returns>
    public static V1Patch GetJsonDiffPatch(
        this IKubernetesObject<V1ObjectMeta> from,
        IKubernetesObject<V1ObjectMeta> to,
        JsonDiffOptions? options = null)
    {
        var patch = from.GetJsonDiff(to, options);
        return new V1Patch(patch.ToString(), V1Patch.PatchType.JsonPatch);
    }
}
