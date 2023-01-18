using FluentAssertions;
using k8s.Models;
using KubeOps.Operator;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Rbac;
using KubeOps.Operator.Webhooks;
using KubeOps.Test.TestEntities;
using Xunit;

namespace KubeOps.Test.Operator.Generators;

public class RbacGeneratorTest
{
    private readonly ComponentRegistrar _componentRegistrar = new();

    [Fact]
    public void Should_Generate_Correct_Rbac_Elements_With_Lease()
    {
        _componentRegistrar.RegisterEntity<TestSpecEntity>();
        _componentRegistrar.RegisterEntity<TestClusterSpecEntity>();
        _componentRegistrar.RegisterEntity<TestStatusEntity>();
        _componentRegistrar.RegisterEntity<EntityWithRbac>();

        var builder = new RbacBuilder(_componentRegistrar, new OperatorSettings());
        var clusterRole = builder.BuildManagerRbac();
        clusterRole.Rules.Should().HaveCount(4);
        clusterRole.Rules.Should().Contain(rule => rule.Resources.Any(resource => resource == "entitywithrbacs"));
        clusterRole.Rules.Should().Contain(rule => rule.Resources.Any(resource => resource == "leases"));
        clusterRole.Rules.Should().Contain(rule => rule.Resources.Any(resource => resource == "deployments"));
        clusterRole.Rules.Should()
            .Contain(rule => rule.Resources.Any(resource => resource == "deployments/status"));
    }

    [Fact]
    public void Should_Generate_Correct_Rbac_Elements_Without_Lease()
    {
        _componentRegistrar.RegisterEntity<TestSpecEntity>();
        _componentRegistrar.RegisterEntity<TestClusterSpecEntity>();
        _componentRegistrar.RegisterEntity<TestStatusEntity>();
        _componentRegistrar.RegisterEntity<EntityWithRbac>();

        var builder = new RbacBuilder(_componentRegistrar, new OperatorSettings { EnableLeaderElection = false });
        var clusterRole = builder.BuildManagerRbac();
        clusterRole.Rules.Should().HaveCount(2);
        clusterRole.Rules.Should().Contain(rule => rule.Resources.Any(resource => resource == "entitywithrbacs"));
    }

    [Fact]
    public void Should_Generate_Base_Rules_When_Able()
    {
        var builder = new RbacBuilder(_componentRegistrar, new OperatorSettings { EnableLeaderElection = false });
        var clusterRole = builder.BuildManagerRbac();
        clusterRole.Rules.Should().HaveCount(1);
        clusterRole.Rules.Should().Contain(rule => rule.Resources.Any(resource => resource == "events"));
    }

    [Fact]
    public void Should_Generate_Only_Lease_Rules_When_Able()
    {
        var builder = new RbacBuilder(_componentRegistrar, new OperatorSettings());
        var clusterRole = builder.BuildManagerRbac();
        clusterRole.Rules.Should().Contain(rule => rule.Resources.Any(resource => resource == "leases"));
        clusterRole.Rules.Should().Contain(rule => rule.Resources.Any(resource => resource == "deployments"));
    }

    [Fact]
    public void Should_Generate_Webhook_Rules_When_Able()
    {
        _componentRegistrar.RegisterValidator<TestWebhook, RbacTest1>();
        var builder = new RbacBuilder(_componentRegistrar, new OperatorSettings { EnableLeaderElection = false });
        var clusterRole = builder.BuildManagerRbac();
        clusterRole.Rules.Should()
            .Contain(
                rule => rule.Resources.Any(resource => resource == "services") &&
                        rule.Resources.Any(resource => resource == "validatingwebhookconfigurations"));
        clusterRole.Rules.Should().Contain(rule => rule.Resources.Any(resource => resource == "services/status"));
    }

    [Fact]
    public void Should_Calculate_Max_Verbs_For_Types()
    {
        _componentRegistrar.RegisterEntity<RbacTest1>();
        var builder = new RbacBuilder(_componentRegistrar, new OperatorSettings { EnableLeaderElection = false });
        var clusterRole = builder.BuildManagerRbac();
        var role = clusterRole.Rules.First();
        role.Resources.Should().Contain("rbactest1s");
        role.Verbs.Should().Contain(new[] { "get", "update", "delete" });
    }

    [Fact]
    public void Should_Correctly_Calculate_All_Verb()
    {
        _componentRegistrar.RegisterEntity<RbacTest2>();
        var builder = new RbacBuilder(_componentRegistrar, new OperatorSettings { EnableLeaderElection = false });
        var clusterRole = builder.BuildManagerRbac();
        var role = clusterRole.Rules.First();
        role.Resources.Should().Contain("rbactest2s");
        role.Verbs.Should().Contain("*").And.HaveCount(1);
    }

    [Fact]
    public void Should_Group_Same_Types_Together()
    {
        _componentRegistrar.RegisterEntity<RbacTest3>();
        var builder = new RbacBuilder(_componentRegistrar, new OperatorSettings { EnableLeaderElection = false });
        var clusterRole = builder.BuildManagerRbac();
        clusterRole.Rules.Should()
            .Contain(
                rule => rule.Resources.Contains("rbactest1s"));
        clusterRole.Rules.Should()
            .Contain(
                rule => rule.Resources.Contains("rbactest2s"));
        clusterRole.Rules.Should().HaveCount(3);
    }

    [Fact]
    public void Should_Group_Types_With_Same_Verbs_Together()
    {
        _componentRegistrar.RegisterEntity<RbacTest4>();
        var builder = new RbacBuilder(_componentRegistrar, new OperatorSettings { EnableLeaderElection = false });
        var clusterRole = builder.BuildManagerRbac();
        clusterRole.Rules.Should()
            .Contain(
                rule => rule.Resources.Contains("rbactest1s") &&
                        rule.Resources.Contains("rbactest4s") &&
                        rule.Verbs.Contains("get") &&
                        rule.Verbs.Contains("update"));
        clusterRole.Rules.Should()
            .Contain(
                rule => rule.Resources.Contains("rbactest2s") &&
                        rule.Resources.Contains("rbactest3s") &&
                        rule.Verbs.Contains("delete"));
        clusterRole.Rules.Should().HaveCount(3);
    }

    [EntityRbac(typeof(EntityWithRbac), Verbs = RbacVerb.All)]
    [KubernetesEntity(Group = "test", ApiVersion = "v1")]
    private class EntityWithRbac : CustomKubernetesEntity<object>
    {
    }

    [KubernetesEntity(Group = "test", ApiVersion = "v1")]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Get)]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Update)]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Delete)]
    private class RbacTest1 : CustomKubernetesEntity<object>
    {
    }

    [KubernetesEntity(Group = "test", ApiVersion = "v1")]
    [EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.Delete)]
    private class RbacTest2 : CustomKubernetesEntity<object>
    {
    }

    [KubernetesEntity(Group = "test", ApiVersion = "v1")]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Get)]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Update)]
    [EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.Delete)]
    private class RbacTest3 : CustomKubernetesEntity<object>
    {
    }

    [KubernetesEntity(Group = "test", ApiVersion = "v1")]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Get)]
    [EntityRbac(typeof(RbacTest1), Verbs = RbacVerb.Update)]
    [EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.Delete)]
    [EntityRbac(typeof(RbacTest2), Verbs = RbacVerb.Delete)]
    [EntityRbac(typeof(RbacTest3), Verbs = RbacVerb.Delete)]
    [EntityRbac(typeof(RbacTest4), Verbs = RbacVerb.Get | RbacVerb.Update)]
    private class RbacTest4 : CustomKubernetesEntity<object>
    {
    }

    private class TestWebhook : IValidationWebhook<RbacTest1>
    {
        public AdmissionOperations Operations => AdmissionOperations.All;
    }
}
