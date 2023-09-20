using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KubeOps.Generator;

public class KubernetesEntitySyntaxReceiver : ISyntaxReceiver
{
    private const string KindName = "Kind";
    private const string GroupName = "Group";
    private const string PluralName = "Plural";
    private const string VersionName = "Version";

    public List<(
        ClassDeclarationSyntax Class,
        (AttributeArgumentSyntax? Version,
        AttributeArgumentSyntax? Kind,
        AttributeArgumentSyntax? Group,
        AttributeArgumentSyntax? Plural) Attribute)> Entities { get; } =
        new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not ClassDeclarationSyntax {AttributeLists.Count: > 0} classDeclarationSyntax ||
            !classDeclarationSyntax.AttributeLists
                .Any(al => al.Attributes
                    .Any(a => a.Name.ToString().Equals("KubernetesEntity", StringComparison.OrdinalIgnoreCase))))
        {
            return;
        }

        var attribute = classDeclarationSyntax.AttributeLists
            .SelectMany(al => al.Attributes)
            .First(a => a.Name.ToString().Equals("KubernetesEntity", StringComparison.OrdinalIgnoreCase));

        Entities.Add((classDeclarationSyntax, (
            attribute.ArgumentList?.Arguments.FirstOrDefault(a =>
                a.NameEquals?.Name.GetText().ToString() == VersionName),
            attribute.ArgumentList?.Arguments.FirstOrDefault(a =>
                a.NameEquals?.Name.GetText().ToString() == KindName),
            attribute.ArgumentList?.Arguments.FirstOrDefault(a =>
                a.NameEquals?.Name.GetText().ToString() == GroupName),
            attribute.ArgumentList?.Arguments.FirstOrDefault(a =>
                a.NameEquals?.Name.GetText().ToString() == PluralName))));
    }
}
