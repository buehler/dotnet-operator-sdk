using FluentAssertions;
using k8s.Models;
using KubeOps.Operator.Webhooks;
using Newtonsoft.Json;
using Xunit;

namespace KubeOps.Test.Operator.Webhook;

public class KubernetesJsonDifferTest
{
    [Fact]
    public void When_diffing_objects_then_kubernetes_naming_conventions_should_be_used()
    {
        var left = new V1Pod { Status = new V1PodStatus(reason: "foo") };
        var right = new V1Pod { Status = new V1PodStatus(reason: "bar") };

        var result = KubernetesJsonDiffer.DiffObjects(left, right);

        // Should be all lowercase.
        result.ToString(Formatting.None)
            .Should()
            .Be("[{\"op\":\"replace\",\"path\":\"/status/reason\",\"value\":\"bar\"}]");
    }
}
