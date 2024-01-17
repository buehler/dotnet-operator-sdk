using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace KubeOps.Generator.Generators;

[Generator]
internal class OperatorBuilderGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var declaration = CompilationUnit()
            .WithUsings(
                List(
                    new List<UsingDirectiveSyntax> { UsingDirective(IdentifierName("KubeOps.Abstractions.Builder")), }))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(ClassDeclaration("OperatorBuilderExtensions")
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .AddMembers(MethodDeclaration(IdentifierName("IOperatorBuilder"), "RegisterComponents")
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
                        ExpressionStatement(
                            InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("builder"),
                                        IdentifierName("RegisterEntities")))),
                        ExpressionStatement(
                            InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("builder"),
                                        IdentifierName("RegisterControllers")))),
                        ExpressionStatement(
                            InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("builder"),
                                        IdentifierName("RegisterFinalizers")))),
                        ReturnStatement(IdentifierName("builder")))))))
            .NormalizeWhitespace();

        context.AddSource(
            "OperatorBuilder.g.cs",
            SourceText.From(declaration.ToString(), Encoding.UTF8, SourceHashAlgorithm.Sha256));
    }
}
