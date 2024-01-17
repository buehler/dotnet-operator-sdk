using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KubeOps.Generator.SyntaxReceiver;

internal class EntityFinalizerSyntaxReceiver : ISyntaxContextReceiver
{
    public List<(ClassDeclarationSyntax Finalizer, string EntityName)> Finalizer { get; } = [];

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax { BaseList.Types.Count: > 0 } cls ||
            cls.BaseList.Types.FirstOrDefault(t => t is
            { Type: GenericNameSyntax { Identifier.Text: "IEntityFinalizer" } }) is not { } baseType)
        {
            return;
        }

        var targetEntity = (baseType.Type as GenericNameSyntax)!.TypeArgumentList.Arguments.First();
        Finalizer.Add((cls, targetEntity.ToString()));
    }
}
