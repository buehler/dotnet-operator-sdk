using FluentAssertions;
using Xunit;

namespace KubeOps.Templates.Test.Templates;

[Collection("Template Tests")]
public class OperatorFSharpTest : IDisposable
{
    private readonly TemplateExecutor _executor = new();

    public OperatorFSharpTest(TemplateInstaller _)
    {
    }

    [Fact]
    public void Should_Create_Correct_Files()
    {
        _executor.ExecuteFSharpTemplate("operator");
        _executor.FileExists("Template.fsproj").Should().BeTrue();
        _executor.FileExists("Startup.fs").Should().BeTrue();
        _executor.FileExists("Program.fs").Should().BeTrue();
        _executor.FileExists("appsettings.Development.json").Should().BeTrue();
        _executor.FileExists("appsettings.json").Should().BeTrue();

        _executor.FileExists("Controller", "DemoController.fs").Should().BeTrue();
        _executor.FileExists("Entities", "V1DemoEntity.fs").Should().BeTrue();
        _executor.FileExists("Finalizer", "DemoFinalizer.fs").Should().BeTrue();
        _executor.FileExists("Webhooks", "DemoValidator.fs").Should().BeTrue();
        _executor.FileExists("Webhooks", "DemoMutator.fs").Should().BeTrue();
    }

    [Fact]
    public void Should_Add_KubeOps_Reference()
    {
        _executor.ExecuteFSharpTemplate("operator-empty");
        _executor.FileContains(@"PackageReference Include=""KubeOps""", "Template.fsproj").Should().BeTrue();
    }

    [Fact]
    public void Should_Add_KubeOps_Reference_Into_Startup_Files()
    {
        _executor.ExecuteFSharpTemplate("operator-empty");
        _executor.FileContains("services.AddKubernetesOperator() |> ignore", "Startup.fs").Should().BeTrue();
        _executor.FileContains("app.UseKubernetesOperator()", "Startup.fs").Should().BeTrue();
    }

    [Fact]
    public void Should_Create_Correct_Program_Code()
    {
        _executor.ExecuteFSharpTemplate("operator-empty");
        _executor.FileContains(".RunOperatorAsync args", "Program.fs")
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Should_Add_Correct_Demo_Files()
    {
        _executor.ExecuteFSharpTemplate("operator");

        _executor.FileContains(
                "inherit CustomKubernetesEntity<V1DemoEntitySpec, V1DemoEntityStatus>()",
                "Entities",
                "V1DemoEntity.fs")
            .Should()
            .BeTrue();
        _executor.FileContains(
                "interface IResourceController<V1DemoEntity> with",
                "Controller",
                "DemoController.fs")
            .Should()
            .BeTrue();
        _executor.FileContains(
                "interface IResourceFinalizer<V1DemoEntity> with",
                "Finalizer",
                "DemoFinalizer.fs")
            .Should()
            .BeTrue();
        _executor.FileContains(
                "interface IValidationWebhook<V1DemoEntity> with",
                "Webhooks",
                "DemoValidator.fs")
            .Should()
            .BeTrue();
        _executor.FileContains(
                "interface IMutationWebhook<V1DemoEntity> with",
                "Webhooks",
                "DemoMutator.fs")
            .Should()
            .BeTrue();
    }

    public void Dispose()
    {
        _executor.Dispose();
    }
}
