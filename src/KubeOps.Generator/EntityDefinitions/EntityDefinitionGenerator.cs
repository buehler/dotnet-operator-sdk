using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace KubeOps.Generator.EntityDefinitions;

[Generator]
public class EntityDefinitionGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new KubernetesEntitySyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not KubernetesEntitySyntaxReceiver receiver)
        {
            return;
        }

        var declaration = CompilationUnit()
            .WithUsings(
                List(
                    new List<UsingDirectiveSyntax>
                    {
                        UsingDirective(IdentifierName("KubeOps.Abstractions.Builder")),
                        UsingDirective(IdentifierName("KubeOps.Abstractions.Entities")),
                    }))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(ClassDeclaration("EntityDefinitions")
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithMembers(
                    List<MemberDeclarationSyntax>(receiver.Entities.Select(e => FieldDeclaration(
                            VariableDeclaration(
                                    IdentifierName("EntityMetadata"))
                                .WithVariables(
                                    SingletonSeparatedList(
                                        VariableDeclarator(e.Class.Identifier)
                                            .WithInitializer(
                                                EqualsValueClause(
                                                    ImplicitObjectCreationExpression()
                                                        .WithArgumentList(
                                                            ArgumentList(
                                                                SeparatedList(new List<ArgumentSyntax>
                                                                {
                                                                    Argument(LiteralExpression(
                                                                        SyntaxKind.StringLiteralExpression,
                                                                        Literal(e.Kind))),
                                                                    Argument(LiteralExpression(
                                                                        SyntaxKind.StringLiteralExpression,
                                                                        Literal(e.Version))),
                                                                    Argument(e.Group switch
                                                                    {
                                                                        null => LiteralExpression(
                                                                            SyntaxKind.NullLiteralExpression),
                                                                        _ => LiteralExpression(
                                                                            SyntaxKind.StringLiteralExpression,
                                                                            Literal(e.Group)),
                                                                    }),
                                                                    Argument(e.Plural switch
                                                                    {
                                                                        null => LiteralExpression(
                                                                            SyntaxKind.NullLiteralExpression),
                                                                        _ => LiteralExpression(
                                                                            SyntaxKind.StringLiteralExpression,
                                                                            Literal(e.Plural)),
                                                                    }),
                                                                }))))))))
                        .WithModifiers(
                            TokenList(
                                Token(SyntaxKind.PublicKeyword),
                                Token(SyntaxKind.StaticKeyword),
                                Token(SyntaxKind.ReadOnlyKeyword))))))
                .AddMembers(MethodDeclaration(IdentifierName("IOperatorBuilder"), "RegisterEntitiyMetadata")
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
                        receiver.Entities
                            .Select(e => ExpressionStatement(
                                InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("builder"),
                                            GenericName(Identifier("AddEntityMetadata"))
                                                .WithTypeArgumentList(
                                                    TypeArgumentList(
                                                        SingletonSeparatedList<TypeSyntax>(
                                                            IdentifierName(context.Compilation
                                                                .GetSemanticModel(e.Class.SyntaxTree)
                                                                .GetDeclaredSymbol(e.Class)!
                                                                .ToDisplayString(SymbolDisplayFormat
                                                                    .FullyQualifiedFormat)))))))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(
                                                    IdentifierName(e.Class.Identifier)))))))
                            .Append<StatementSyntax>(ReturnStatement(IdentifierName("builder"))))))))
            .NormalizeWhitespace();

        context.AddSource("EntityDefinitions.g.cs", $"// <auto-generated/>\n\n{declaration}");
    }
}
