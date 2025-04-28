using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KubeOps.Generator.SyntaxReceiver;

internal sealed class EntityControllerSyntaxReceiver : ISyntaxContextReceiver
{
    private const string IEntityControllerMetadataName = "KubeOps.Abstractions.Controller.IEntityController`1";

    public List<(ClassDeclarationSyntax Controller, string EntityName)> Controllers { get; } = [];

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
        {
            return;
        }

        if (classSymbol.IsAbstract)
        {
            return;
        }

        var iEntityControllerInterface = context.SemanticModel.Compilation.GetTypeByMetadataName(IEntityControllerMetadataName);
        if (iEntityControllerInterface is null)
        {
            return;
        }

        var implementedControllerInterface = classSymbol.AllInterfaces
            .FirstOrDefault(i => i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iEntityControllerInterface));

        var entityTypeSymbol = implementedControllerInterface?.TypeArguments.FirstOrDefault();

        if (entityTypeSymbol == null)
        {
            return;
        }

        Controllers.Add((classDeclarationSyntax, entityTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }
}
