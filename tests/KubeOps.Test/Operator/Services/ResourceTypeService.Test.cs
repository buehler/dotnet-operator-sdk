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
        private readonly ResourceLocator _currentAssemblyResourceTypeService =
            new(Assembly.GetExecutingAssembly());

        private readonly ResourceLocator _testAssembliesResourceTypeService =
            new(Assembly.GetExecutingAssembly(), Assembly.GetAssembly(typeof(V1TestEntity))!);

        [Fact]
        public void Should_Return_Correct_Number_Of_KubernetesEntity_Types()
        {
            var currentAssemblyTypes =
                _currentAssemblyResourceTypeService.GetTypesWithAttribute<KubernetesEntityAttribute>();
            var testAssembliesTypes =
                _testAssembliesResourceTypeService.GetTypesWithAttribute<KubernetesEntityAttribute>();

            currentAssemblyTypes.Count().Should().Be(11);
            testAssembliesTypes.Count().Should().Be(14);
        }

        [Fact]
        public void Should_Return_Correct_Number_Of_IgnoreEntity_Types()
        {
            var currentAssemblyTypes =
                _currentAssemblyResourceTypeService.GetTypesWithAttribute<IgnoreEntityAttribute>();
            var testAssembliesTypes =
                _testAssembliesResourceTypeService.GetTypesWithAttribute<IgnoreEntityAttribute>();

            currentAssemblyTypes.Count().Should().Be(2);
            testAssembliesTypes.Count().Should().Be(2);
        }

        [Fact]
        public void Should_Return_Correct_Number_Of_KubernetesEntityAttribute_Instances()
        {
            var currentAssemblyAttributes =
                _currentAssemblyResourceTypeService.GetAttributes<KubernetesEntityAttribute>();
            var testAssembliesAttributes =
                _testAssembliesResourceTypeService.GetAttributes<KubernetesEntityAttribute>();

            currentAssemblyAttributes.Count().Should().Be(11);
            testAssembliesAttributes.Count().Should().Be(14);
        }

        [Fact]
        public void Should_Return_Correct_Number_Of_IgnoreEntityAttribute_Instances()
        {
            var currentAssemblyAttributes =
                _currentAssemblyResourceTypeService.GetAttributes<IgnoreEntityAttribute>();
            var testAssembliesAttributes =
                _testAssembliesResourceTypeService.GetAttributes<IgnoreEntityAttribute>();

            currentAssemblyAttributes.Count().Should().Be(2);
            testAssembliesAttributes.Count().Should().Be(2);
        }
    }
}
