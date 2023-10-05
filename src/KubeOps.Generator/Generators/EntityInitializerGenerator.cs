using System.Text;

using KubeOps.Generator.SyntaxReceiver;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace KubeOps.Generator.Generators;

[Generator]
internal class EntityInitializerGenerator : ISourceGenerator
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

        // for each partial defined entity, create a partial class that
        // introduces a default constructor that initializes the ApiVersion and Kind.
        // But only, if there is no default constructor defined.
        foreach (var entity in receiver.Entities
                     .Where(e => e.Class.Modifiers.Any(SyntaxKind.PartialKeyword))
                     .Where(e => !e.Class.Members.Any(m => m is ConstructorDeclarationSyntax
                     {
                         ParameterList.Parameters.Count: 0,
                     })))
        {
            var symbol = context.Compilation
                .GetSemanticModel(entity.Class.SyntaxTree)
                .GetDeclaredSymbol(entity.Class)!;

            var ns = new List<MemberDeclarationSyntax>();
            if (!symbol.ContainingNamespace.IsGlobalNamespace)
            {
                ns.Add(FileScopedNamespaceDeclaration(IdentifierName(symbol.ContainingNamespace.ToDisplayString())));
            }

            var partialEntityInitializer = CompilationUnit()
                .AddMembers(ns.ToArray())
                .AddMembers(ClassDeclaration(entity.Class.Identifier)
                    .WithModifiers(entity.Class.Modifiers)
                    .AddMembers(ConstructorDeclaration(entity.Class.Identifier)
                        .WithModifiers(
                            TokenList(
                                Token(SyntaxKind.PublicKeyword)))
                        .WithBody(
                            Block(
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName("ApiVersion"),
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal($"{entity.Group}/{entity.Version}".TrimStart('/'))))),
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName("Kind"),
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal(entity.Kind))))))))
                .NormalizeWhitespace();

            context.AddSource(
                $"{entity.Class.Identifier}.init.g.cs",
                SourceText.From(partialEntityInitializer.ToString(), Encoding.UTF8, SourceHashAlgorithm.Sha256));
        }

        // for each NON partial entity, generate a method extension that initializes the ApiVersion and Kind.
        var staticInitializers = CompilationUnit()
            .WithMembers(SingletonList<MemberDeclarationSyntax>(ClassDeclaration("EntityInitializer")
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithMembers(List<MemberDeclarationSyntax>(receiver.Entities
                    .Where(e => !e.Class.Modifiers.Any(SyntaxKind.PartialKeyword) || e.Class.Members.Any(m =>
                        m is ConstructorDeclarationSyntax
                        {
                            ParameterList.Parameters.Count: 0,
                        }))
                    .Select(e => (Entity: e,
                        ClassIdentifier: context.Compilation.GetSemanticModel(e.Class.SyntaxTree)
                            .GetDeclaredSymbol(e.Class)!.ToDisplayString(SymbolDisplayFormat
                                .FullyQualifiedFormat)))
                    .Select(e =>
                        MethodDeclaration(
                                IdentifierName(e.ClassIdentifier),
                                "Initialize")
                            .WithModifiers(
                                TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                            .WithParameterList(ParameterList(
                                SingletonSeparatedList(
                                    Parameter(
                                            Identifier("entity"))
                                        .WithModifiers(
                                            TokenList(
                                                Token(SyntaxKind.ThisKeyword)))
                                        .WithType(IdentifierName(e.ClassIdentifier)))))
                            .WithBody(Block(
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("entity"),
                                            IdentifierName("ApiVersion")),
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal($"{e.Entity.Group}/{e.Entity.Version}".TrimStart('/'))))),
                                ExpressionStatement(
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("entity"),
                                            IdentifierName("Kind")),
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal(e.Entity.Kind)))),
                                ReturnStatement(IdentifierName("entity")))))))))
            .NormalizeWhitespace();

        context.AddSource(
            "EntityInitializer.g.cs",
            SourceText.From(staticInitializers.ToString(), Encoding.UTF8, SourceHashAlgorithm.Sha256));
    }
}
