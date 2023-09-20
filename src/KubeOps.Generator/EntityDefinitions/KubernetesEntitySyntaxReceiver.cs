using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KubeOps.Generator.EntityDefinitions;

public class KubernetesEntitySyntaxReceiver : ISyntaxContextReceiver
{
    private const string KindName = "Kind";
    private const string GroupName = "Group";
    private const string PluralName = "Plural";
    private const string VersionName = "ApiVersion";
    private const string DefaultVersion = "v1";

    public List<AttributedEntity> Entities { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax { AttributeLists.Count: > 0 } cls ||
            cls.AttributeLists.SelectMany(a => a.Attributes)
                .FirstOrDefault(a => a.Name.ToString() == "KubernetesEntity") is not { } attr)
        {
            return;
        }

        Entities.Add(new(
            cls,
            GetArgumentValue(context.SemanticModel, attr, KindName) ?? cls.Identifier.ToString(),
            GetArgumentValue(context.SemanticModel, attr, VersionName) ?? DefaultVersion,
            GetArgumentValue(context.SemanticModel, attr, GroupName),
            GetArgumentValue(context.SemanticModel, attr, PluralName)));
    }

    private string? GetArgumentValue(SemanticModel model, AttributeSyntax attr, string argName)
    {
        if (attr.ArgumentList?.Arguments.FirstOrDefault(a => a.NameEquals?.Name.ToString() == argName) is { } arg)
        {
            return model.GetOperation(arg.Expression)?.ConstantValue.Value?.ToString();
        }

        return null;
    }
}
