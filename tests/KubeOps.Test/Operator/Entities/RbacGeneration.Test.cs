using KubeOps.Operator.Commands.Generators;
using KubeOps.Test.Operator.Entities.TestEntities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Xunit;

namespace KubeOps.Test.Operator.Entities
{
    public class RbacGenerationTest
    {
        private readonly Assembly _testSpecAssembly = typeof(TestSpecEntity).Assembly;

        [Fact]
        public void Should_Get_RbacRole_From_Assembly()
        {
            var result = RbacGenerator.GetRbacRole(_testSpecAssembly);

            Assert.Equal("test-role", result);
        }

        [Fact]
        public void Should_Generate_Role_With_Name()
        {
            var result = RbacGenerator.GenerateManagerRbac(_testSpecAssembly, "test-operator");

            Assert.Equal("test-operator", result.Metadata.Name);
        }


        [Fact]
        public void Should_Generate_RoleBinding_With_Name()
        {
            var result = RbacGenerator.GenerateRoleBinding("test-operator");

            Assert.Equal("test-operator-binding", result.Metadata.Name);
        }

        [Fact]
        public void Should_Generate_RoleBinding_With_RoleReference()
        {
            var result = RbacGenerator.GenerateRoleBinding("test-operator");

            Assert.Equal("test-operator", result.RoleRef.Name);
        }

    }
}
