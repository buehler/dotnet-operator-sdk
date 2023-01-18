using FluentAssertions;
using KubeOps.Operator.Builder;
using KubeOps.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KubeOps.TestOperator.Test;

public class AssemblyScannerTest : IClassFixture<KubernetesOperatorFactory<TestAssemblyScannedStartup>>
{
    private readonly KubernetesOperatorFactory<TestAssemblyScannedStartup> _factory;
    private readonly IComponentRegistrar _componentRegistrar;

    public AssemblyScannerTest(KubernetesOperatorFactory<TestAssemblyScannedStartup> factory)
    {
        _factory = factory.WithSolutionRelativeContentRoot("tests/KubeOps.TestOperator");
        _componentRegistrar = _factory.Services.GetRequiredService<IComponentRegistrar>();
    }

    [Fact]
    public void Should_Load_Correct_Number_Of_Entities()
    {
        _componentRegistrar.EntityRegistrations
            .Select(r => r.EntityType)
            .Distinct()
            .Count()
            .Should()
            .Be(3);
    }

    [Fact]
    public void Should_Load_Correct_Number_Of_Controllers()
    {
        _componentRegistrar.ControllerRegistrations
            .Select(r => r.ControllerType)
            .Distinct()
            .Count()
            .Should()
            .Be(1);
    }

    [Fact]
    public void Should_Load_Correct_Number_Of_Finalizers()
    {
        _componentRegistrar.FinalizerRegistrations
            .Select(r => r.FinalizerType)
            .Distinct()
            .Count()
            .Should()
            .Be(1);
    }

    [Fact]
    public void Should_Load_Correct_Number_Of_Validators()
    {
        _componentRegistrar.ValidatorRegistrations
            .Select(r => r.ValidatorType)
            .Distinct()
            .Count()
            .Should()
            .Be(1);
    }

    [Fact]
    public void Should_Load_Correct_Number_Of_Mutators()
    {
        _componentRegistrar.MutatorRegistrations
            .Select(r => r.MutatorType)
            .Distinct()
            .Count()
            .Should()
            .Be(1);
    }
}
