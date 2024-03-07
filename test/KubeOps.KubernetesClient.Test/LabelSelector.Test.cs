﻿using KubeOps.KubernetesClient.LabelSelectors;

namespace KubeOps.KubernetesClient.Test;

public class LabelSelectorTest : IntegrationTestBase
{
    [Fact]
    public void Sould_Return_Correct_Expression()
    {
        var labelSelectors = new LabelSelector[] {
            new EqualsSelector("app", Enumerable.Range(0,3).Select(x=>$"app-{x}").ToArray()),
            new NotEqualsSelector("srv", Enumerable.Range(0,2).Select(x=>$"service-{x}").ToArray())
        };

        string expected = "app in (app-0,app-1,app-2),srv notin (service-0,service-1)";
        var actual = labelSelectors.ToExpression();
        Assert.Equal(expected, actual);
    }
}
