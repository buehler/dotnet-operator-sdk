using FluentAssertions;
using k8s.Models;
using KubeOps.Operator.Webhooks;
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
        result.ToJsonString()
            .Should()
            .Be("[{\"op\":\"replace\",\"path\":\"/status/reason\",\"value\":\"bar\"}]");
    }

    [Fact]
    public void When_diffing_null_objects_then_no_errors_should_be_thrown()
    {
        var result = KubernetesJsonDiffer.DiffObjects(null, null);

        Assert.NotNull(result);
    }
}
