using KubeOps.Operator.Commands.Generators;
using KubeOps.Test.Operator.Entities.TestEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace KubeOps.Test.Operator.Entities
{
    public class OperatorGenerationTest
    {
        private readonly Assembly _testSpecAssembly = typeof(TestSpecEntity).Assembly;

        [Fact]
        public void Should_Get_OperatorName_From_Assembly()
        {
            var result = OperatorGenerator.OperatorName(_testSpecAssembly);

            Assert.Equal("test-operator", result);
        }

        [Fact]
        public void Should_Generate_Deployment_With_Name()
        {
            var result = OperatorGenerator.GenerateDeployment(_testSpecAssembly);

            Assert.Equal("test-operator", result.Metadata.Name);
        }


        [Fact]
        public void Should_Generate_Image_Name()
        {
            var result = OperatorGenerator.GenerateDeployment(_testSpecAssembly);

            Assert.Equal("some.azurecr.io/test-operator", result.Spec.Template.Spec.Containers.First().Image);
        }

        [Fact]
        public void Should_Generate_With_ImagePullSecretName()
        {
            var result = OperatorGenerator.GenerateDeployment(_testSpecAssembly);

            Assert.Equal("test-secret-name", result.Spec.Template.Spec.ImagePullSecrets.First().Name);
        }

    }
}
