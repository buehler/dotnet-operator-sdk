using Xunit;

namespace KubeOps.Templates.Test.Templates;

[CollectionDefinition("Template Tests")]
public class TemplateTestCollection : ICollectionFixture<TemplateInstaller>
{
}
