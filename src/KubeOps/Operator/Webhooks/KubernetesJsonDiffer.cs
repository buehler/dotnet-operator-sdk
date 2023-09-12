using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
using System.Text.Json.Nodes;
using k8s;

namespace KubeOps.Operator.Webhooks;

internal static class KubernetesJsonDiffer
{
    private static readonly JsonPatchDeltaFormatter Formatter = new();

    public static JsonNode? DiffObjects(object? from, object? to)
    {
        var fromToken = GetJToken(from);
        var toToken = GetJToken(to);

        return fromToken.Diff(toToken, Formatter);
    }

    private static JsonNode? GetJToken(object? o)
    {
        // Use the K8s Serializer to ensure we match their naming conventions
        // (and handle object conversions correctly).
        var json = KubernetesJson.Serialize(o);
        return JsonNode.Parse(json);
    }
}
