using System.Text;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
using System.Text.Json.Nodes;

using k8s;

namespace KubeOps.Operator.Web.Webhooks.Admission.Mutation;

internal static class JsonDiffer
{
    public static string Base64Diff(this JsonNode from, object? to)
    {
        var formatter = new JsonPatchDeltaFormatter();

        var toToken = GetNode(to);
        var patch = from.Diff(toToken, formatter)!;

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(patch.ToString()));
    }

    public static JsonNode? GetNode(object? o)
    {
        var json = KubernetesJson.Serialize(o);
        return JsonNode.Parse(json);
    }
}
