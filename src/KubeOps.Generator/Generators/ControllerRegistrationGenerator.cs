using System.Text;

using KubeOps.Generator.SyntaxReceiver;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace KubeOps.Generator.Generators;

[Generator]
internal class ControllerRegistrationGenerator : ISourceGenerator
{
    private readonly EntityControllerSyntaxReceiver _ctrlReceiver = new();
    private readonly KubernetesEntitySyntaxReceiver _entityReceiver = new();

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new CombinedSyntaxReceiver(_ctrlReceiver, _entityReceiver));
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not CombinedSyntaxReceiver)
        {
            return;
        }

        var declaration = CompilationUnit()
            .WithUsings(
                List(
                    new List<UsingDirectiveSyntax> { UsingDirective(IdentifierName("KubeOps.Abstractions.Builder")), }))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(ClassDeclaration("ControllerRegistrations")
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .AddMembers(MethodDeclaration(IdentifierName("IOperatorBuilder"), "RegisterControllers")
                    .WithModifiers(
                        TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                    .WithParameterList(ParameterList(
                        SingletonSeparatedList(
                            Parameter(
                                    Identifier("builder"))
                                .WithModifiers(
                                    TokenList(
                                        Token(SyntaxKind.ThisKeyword)))
                                .WithType(
                                    IdentifierName("IOperatorBuilder")))))
                    .WithBody(Block(
                        _ctrlReceiver.Controllers
                            .Where(c => _entityReceiver.Entities.Exists(e =>
                                e.Class.Identifier.ToString() == c.EntityName))
                            .Select(c => (c.Controller, Entity: _entityReceiver.Entities.First(e =>
                                e.Class.Identifier.ToString() == c.EntityName).Class))
                            .Select(e => ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("builder"),
                                        GenericName(Identifier("AddController"))
                                            .WithTypeArgumentList(
                                                TypeArgumentList(
                                                    SeparatedList<TypeSyntax>(new[]
                                                    {
                                                        IdentifierName(context.Compilation
                                                            .GetSemanticModel(e.Controller.SyntaxTree)
                                                            .GetDeclaredSymbol(e.Controller)!
                                                            .ToDisplayString(SymbolDisplayFormat
                                                                .FullyQualifiedFormat)),
                                                        IdentifierName(context.Compilation
                                                            .GetSemanticModel(e.Entity.SyntaxTree)
                                                            .GetDeclaredSymbol(e.Entity)!
                                                            .ToDisplayString(SymbolDisplayFormat
                                                                .FullyQualifiedFormat)),
                                                    })))))))
                            .Append<StatementSyntax>(ReturnStatement(IdentifierName("builder"))))))))
            .NormalizeWhitespace();

        context.AddSource(
            "ControllerRegistrations.g.cs",
            SourceText.From(declaration.ToString(), Encoding.UTF8, SourceHashAlgorithm.Sha256));
    }
}
