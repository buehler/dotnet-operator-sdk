using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KubeOps.Generator.SyntaxReceiver;

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
            GetArgumentValue(attr, KindName) ?? cls.Identifier.ToString(),
            GetArgumentValue(attr, VersionName) ?? DefaultVersion,
            GetArgumentValue(attr, GroupName),
            GetArgumentValue(attr, PluralName)));
    }

    private static string? GetArgumentValue(AttributeSyntax attr, string argName) =>
        attr.ArgumentList?.Arguments.FirstOrDefault(a => a.NameEquals?.Name.ToString() == argName) is
        { Expression: LiteralExpressionSyntax { Token.ValueText: { } value } }
            ? value
            : null;
}
