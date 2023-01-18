using FluentAssertions;
using Xunit;

namespace KubeOps.Templates.Test.Templates;

[Collection("Template Tests")]
public class EmptyCSharpTest : IDisposable
{
    private readonly TemplateExecutor _executor = new();

    public EmptyCSharpTest(TemplateInstaller _)
    {
    }

    [Fact]
    public void Should_Create_Correct_Files()
    {
        _executor.ExecuteCSharpTemplate("operator-empty");
        _executor.FileExists("Template.csproj").Should().BeTrue();
        _executor.FileExists("Program.cs").Should().BeTrue();
        _executor.FileExists("appsettings.Development.json").Should().BeTrue();
        _executor.FileExists("appsettings.json").Should().BeTrue();
    }

    [Fact]
    public void Should_Add_KubeOps_Reference()
    {
        _executor.ExecuteCSharpTemplate("operator-empty");
        _executor.FileContains(@"PackageReference Include=""KubeOps""", "Template.csproj").Should().BeTrue();
    }

    [Fact]
    public void Should_Add_KubeOps_Reference_Into_Program_Code()
    {
        _executor.ExecuteCSharpTemplate("operator-empty");
        _executor.FileContains("builder.Services.AddKubernetesOperator();", "Program.cs").Should().BeTrue();
        _executor.FileContains("app.UseKubernetesOperator();", "Program.cs").Should().BeTrue();
        _executor.FileContains("await app.RunOperatorAsync(args);", "Program.cs").Should().BeTrue();
    }

    public void Dispose()
    {
        _executor.Dispose();
    }
}
