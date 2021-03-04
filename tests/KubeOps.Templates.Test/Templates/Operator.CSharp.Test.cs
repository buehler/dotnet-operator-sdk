using System;
using FluentAssertions;
using Xunit;

namespace KubeOps.Templates.Test.Templates
{
    [Collection("Template Tests")]
    public class OperatorCSharpTest : IDisposable
    {
        private readonly TemplateExecutor _executor = new();

        public OperatorCSharpTest(TemplateInstaller _)
        {
        }

        [Fact]
        public void Should_Create_Correct_Files()
        {
            _executor.ExecuteCSharpTemplate("operator");
            _executor.FileExists("Template.csproj").Should().BeTrue();
            _executor.FileExists("Startup.cs").Should().BeTrue();
            _executor.FileExists("Program.cs").Should().BeTrue();
            _executor.FileExists("appsettings.Development.json").Should().BeTrue();
            _executor.FileExists("appsettings.json").Should().BeTrue();

            _executor.FileExists("Controller", "DemoController.cs").Should().BeTrue();
            _executor.FileExists("Entities", "V1DemoEntity.cs").Should().BeTrue();
            _executor.FileExists("Finalizer", "DemoFinalizer.cs").Should().BeTrue();
            _executor.FileExists("Webhooks", "DemoValidator.cs").Should().BeTrue();
            _executor.FileExists("Webhooks", "DemoMutator.cs").Should().BeTrue();
        }

        [Fact]
        public void Should_Add_KubeOps_Reference()
        {
            _executor.ExecuteCSharpTemplate("operator");
            _executor.FileContains(@"PackageReference Include=""KubeOps""", "Template.csproj").Should().BeTrue();
        }

        [Fact]
        public void Should_Add_KubeOps_Reference_Into_Startup_Files()
        {
            _executor.ExecuteCSharpTemplate("operator");
            _executor.FileContains("services.AddKubernetesOperator();", "Startup.cs").Should().BeTrue();
            _executor.FileContains("app.UseKubernetesOperator();", "Startup.cs").Should().BeTrue();
        }

        [Fact]
        public void Should_Create_Correct_Program_Code()
        {
            _executor.ExecuteCSharpTemplate("operator");
            _executor.FileContains("await CreateHostBuilder(args).Build().RunOperatorAsync(args);", "Program.cs")
                .Should()
                .BeTrue();
        }

        [Fact]
        public void Should_Add_Correct_Demo_Files()
        {
            _executor.ExecuteCSharpTemplate("operator");

            _executor.FileContains(
                    "public class V1DemoEntity : CustomKubernetesEntity<V1DemoEntity.V1DemoEntitySpec, V1DemoEntity.V1DemoEntityStatus>",
                    "Entities",
                    "V1DemoEntity.cs")
                .Should()
                .BeTrue();
            _executor.FileContains(
                    "public class DemoController : IResourceController<V1DemoEntity>",
                    "Controller",
                    "DemoController.cs")
                .Should()
                .BeTrue();
            _executor.FileContains(
                    "public class DemoFinalizer : IResourceFinalizer<V1DemoEntity>",
                    "Finalizer",
                    "DemoFinalizer.cs")
                .Should()
                .BeTrue();
            _executor.FileContains(
                    "public class DemoValidator : IValidationWebhook<V1DemoEntity>",
                    "Webhooks",
                    "DemoValidator.cs")
                .Should()
                .BeTrue();
            _executor.FileContains(
                    "public class DemoMutator : IMutationWebhook<V1DemoEntity>",
                    "Webhooks",
                    "DemoMutator.cs")
                .Should()
                .BeTrue();
        }

        public void Dispose()
        {
            _executor.Dispose();
        }
    }
}
