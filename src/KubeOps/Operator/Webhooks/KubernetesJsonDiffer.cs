using JsonDiffPatch;
using k8s;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KubeOps.Operator.Webhooks;

internal static class KubernetesJsonDiffer
{
    private static readonly JsonDiffer JsonDiffer = new();

    public static PatchDocument DiffObjects(object? from, object? to)
    {
        var fromToken = GetJToken(from);
        var toToken = GetJToken(to);

        return JsonDiffer.Diff(fromToken, toToken, false);
    }

    private static JToken GetJToken(object? o)
    {
        // Use the K8s Serializer to ensure we match their naming conventions
        // (and handle object conversions correctly).
        var json = KubernetesJson.Serialize(o);
        return JToken.ReadFrom(new JsonTextReader(new StringReader(json)));
    }
}
