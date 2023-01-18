using FluentAssertions;
using KubeOps.Operator.Finalizer;
using KubeOps.Test.TestEntities;
using Xunit;

namespace KubeOps.Test.Operator.Finalizer;

public class ResourceFinalizerTest
{
    [Fact]
    public void Should_Not_Add_Finalizer_Suffix()
    {
        var finalizer = new MyFirstFinalizer() as IResourceFinalizer<TestSpecEntity>;
        finalizer.Identifier.Should().Be("kubeops.test.dev/myfirstfinalizer");
    }

    [Fact]
    public void Should_Add_Finalizer_Suffix()
    {
        var finalizer = new MyFirst() as IResourceFinalizer<TestSpecEntity>;
        finalizer.Identifier.Should().Be("kubeops.test.dev/myfirstfinalizer");
    }

    [Fact]
    public void Should_Truncate_The_Identifier()
    {
        var finalizer =
            new ThisIsAVeryLongFinalizerNameForAFinalizerAlrightNowIGotIt() as IResourceFinalizer<TestSpecEntity>;
        finalizer.Identifier.Should().Be("kubeops.test.dev/thisisaverylongfinalizernameforafinalizeralrig");
    }

    private class MyFirstFinalizer : IResourceFinalizer<TestSpecEntity>
    {
        public Task FinalizeAsync(TestSpecEntity entity) => Task.CompletedTask;
    }

    private class MyFirst : IResourceFinalizer<TestSpecEntity>
    {
        public Task FinalizeAsync(TestSpecEntity entity) => Task.CompletedTask;
    }

    private class ThisIsAVeryLongFinalizerNameForAFinalizerAlrightNowIGotIt : IResourceFinalizer<TestSpecEntity>
    {
        public Task FinalizeAsync(TestSpecEntity entity) => Task.CompletedTask;
    }
}
