using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KubeOps.Generator.SyntaxReceiver;

internal class EntityControllerSyntaxReceiver : ISyntaxContextReceiver
{
    public List<(ClassDeclarationSyntax Controller, string EntityName)> Controllers { get; } = [];

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax { BaseList.Types.Count: > 0 } cls ||
            cls.BaseList.Types.FirstOrDefault(t => t is
            { Type: GenericNameSyntax { Identifier.Text: "IEntityController" } }) is not { } baseType)
        {
            return;
        }

        var targetEntity = (baseType.Type as GenericNameSyntax)!.TypeArgumentList.Arguments.First();
        Controllers.Add((cls, targetEntity.ToString()));
    }
}
