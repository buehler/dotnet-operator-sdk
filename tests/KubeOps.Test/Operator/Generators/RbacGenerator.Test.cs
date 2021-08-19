using System.Linq;
using FluentAssertions;
using k8s.Models;
using KubeOps.Operator;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Entities;
using KubeOps.Operator.Rbac;
using KubeOps.Test.TestEntities;
using Xunit;

namespace KubeOps.Test.Operator.Generators
{
    public class RbacGeneratorTest
    {
        private readonly ComponentRegistrar _componentRegistrar = new();

        public RbacGeneratorTest()
        {
            _componentRegistrar.RegisterEntity<TestSpecEntity>();
            _componentRegistrar.RegisterEntity<TestClusterSpecEntity>();
            _componentRegistrar.RegisterEntity<TestStatusEntity>();
            _componentRegistrar.RegisterEntity<EntityWithRbac>();
        }

        [Fact]
        public void Should_Generate_Correct_Rbac_Elements_With_Lease()
        {
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
            var builder = new RbacBuilder(_componentRegistrar, new OperatorSettings { EnableLeaderElection = false });
            var clusterRole = builder.BuildManagerRbac();
            clusterRole.Rules.Should().HaveCount(1);
            clusterRole.Rules.Should().Contain(rule => rule.Resources.Any(resource => resource == "entitywithrbacs"));
        }

        [EntityRbac(typeof(EntityWithRbac), Verbs = RbacVerb.All)]
        [KubernetesEntity(Group = "test", ApiVersion = "v1")]
        private class EntityWithRbac : CustomKubernetesEntity<object>
        {
        }
    }
}
