using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KubeOps.Operator.Services;
using KubeOps.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KubeOps.TestOperator.Test
{
    public class AssemblyScannerTest : IClassFixture<KubernetesOperatorFactory<TestAssemblyScannedStartup>>
    {
        private readonly KubernetesOperatorFactory<TestAssemblyScannedStartup> _factory;

        public AssemblyScannerTest(KubernetesOperatorFactory<TestAssemblyScannedStartup> factory)
        {
            _factory = factory.WithSolutionRelativeContentRoot("tests/KubeOps.TestOperator");
        }

        [Fact]
        public void Should_Load_Correct_Number_Of_Entities()
        {
            var entities = _factory.Services.GetRequiredService<IEnumerable<EntityType>>();
            entities.Distinct().Count().Should().Be(3);
        } 

        [Fact]
        public void Should_Load_Correct_Number_Of_Controllers()
        {
            var controllers = _factory.Services.GetRequiredService<IEnumerable<ControllerType>>();
            controllers.Distinct().Count().Should().Be(1);
        } 

        [Fact]
        public void Should_Load_Correct_Number_Of_Finalizers()
        {
            var finalizers = _factory.Services.GetRequiredService<IEnumerable<FinalizerType>>();
            finalizers.Distinct().Count().Should().Be(1);
        } 

        [Fact]
        public void Should_Load_Correct_Number_Of_Validators()
        {
            var validators = _factory.Services.GetRequiredService<IEnumerable<ValidatorType>>();
            validators.Distinct().Count().Should().Be(1);
        } 

        [Fact]
        public void Should_Load_Correct_Number_Of_Mutators()
        {
            var mutators = _factory.Services.GetRequiredService<IEnumerable<MutatorType>>();
            mutators.Distinct().Count().Should().Be(1);
        } 
    }
}
