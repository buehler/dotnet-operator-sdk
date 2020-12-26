using System.Linq;
using System.Reflection;
using FluentAssertions;
using k8s.Models;
using KubeOps.Operator.Entities.Annotations;
using KubeOps.Operator.Services;
using KubeOps.TestOperator.Entities;
using Xunit;

namespace KubeOps.Test.Operator.Services
{
    public class ResourceTypeServiceTest
    {
        private readonly IResourceTypeService _currentAssemblyResourceTypeService =
            new ResourceTypeService(Assembly.GetExecutingAssembly());

        private readonly IResourceTypeService _testAssembliesResourceTypeService =
            new ResourceTypeService(Assembly.GetExecutingAssembly(), Assembly.GetAssembly(typeof(V1TestEntity))!);

        [Fact]
        public void Should_Return_Correct_Number_Of_KubernetesEntity_Types()
        {
            var currentAssemblyTypes =
                _currentAssemblyResourceTypeService.GetResourceTypesByAttribute<KubernetesEntityAttribute>();
            var testAssembliesTypes =
                _testAssembliesResourceTypeService.GetResourceTypesByAttribute<KubernetesEntityAttribute>();

            currentAssemblyTypes.Count().Should().Be(11);
            testAssembliesTypes.Count().Should().Be(13);
        }

        [Fact]
        public void Should_Return_Correct_Number_Of_IgnoreEntity_Types()
        {
            var currentAssemblyTypes =
                _currentAssemblyResourceTypeService.GetResourceTypesByAttribute<IgnoreEntityAttribute>();
            var testAssembliesTypes =
                _testAssembliesResourceTypeService.GetResourceTypesByAttribute<IgnoreEntityAttribute>();

            currentAssemblyTypes.Count().Should().Be(2);
            testAssembliesTypes.Count().Should().Be(2);
        }

        [Fact]
        public void Should_Return_Correct_Number_Of_KubernetesEntityAttribute_Instances()
        {
            var currentAssemblyAttributes =
                _currentAssemblyResourceTypeService.GetResourceAttributes<KubernetesEntityAttribute>();
            var testAssembliesAttributes =
                _testAssembliesResourceTypeService.GetResourceAttributes<KubernetesEntityAttribute>();

            currentAssemblyAttributes.Count().Should().Be(11);
            testAssembliesAttributes.Count().Should().Be(13);
        }

        [Fact]
        public void Should_Return_Correct_Number_Of_IgnoreEntityAttribute_Instances()
        {
            var currentAssemblyAttributes =
                _currentAssemblyResourceTypeService.GetResourceAttributes<IgnoreEntityAttribute>();
            var testAssembliesAttributes =
                _testAssembliesResourceTypeService.GetResourceAttributes<IgnoreEntityAttribute>();

            currentAssemblyAttributes.Count().Should().Be(2);
            testAssembliesAttributes.Count().Should().Be(2);
        }
    }
}
