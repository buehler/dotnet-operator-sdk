using System.Text;

using KubeOps.Generator.SyntaxReceiver;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace KubeOps.Generator.Generators;

[Generator]
internal class FinalizerRegistrationGenerator : ISourceGenerator
{
    private const byte MaxNameLength = 63;

    private readonly EntityFinalizerSyntaxReceiver _finalizerReceiver = new();
    private readonly KubernetesEntitySyntaxReceiver _entityReceiver = new();

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new CombinedSyntaxReceiver(_finalizerReceiver, _entityReceiver));
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not CombinedSyntaxReceiver)
        {
            return;
        }

        var finalizers = _finalizerReceiver.Finalizer
            .Where(c => _entityReceiver.Entities.Exists(e =>
                e.Class.Identifier.ToString() == c.EntityName))
            .Select(c => (c.Finalizer, Entity: _entityReceiver.Entities.First(e =>
                e.Class.Identifier.ToString() == c.EntityName))).ToList();

        var declaration = CompilationUnit()
            .WithUsings(
                List(
                    new List<UsingDirectiveSyntax> { UsingDirective(IdentifierName("KubeOps.Abstractions.Builder")), }))
            .WithMembers(SingletonList<MemberDeclarationSyntax>(ClassDeclaration("FinalizerRegistrations")
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithMembers(List<MemberDeclarationSyntax>(finalizers.Select(f =>
                    FieldDeclaration(
                            VariableDeclaration(
                                    PredefinedType(
                                        Token(SyntaxKind.StringKeyword)))
                                .WithVariables(
                                    SingletonSeparatedList(
                                        VariableDeclarator(Identifier($"{f.Finalizer.Identifier}Identifier"))
                                            .WithInitializer(
                                                EqualsValueClause(
                                                    LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        Literal(FinalizerName(f))))))))
                        .WithModifiers(
                            TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ConstKeyword))))))
                .AddMembers(MethodDeclaration(IdentifierName("IOperatorBuilder"), "RegisterFinalizers")
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
                        finalizers.Select(f => ExpressionStatement(
                                InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("builder"),
                                            GenericName(Identifier("AddFinalizer"))
                                                .WithTypeArgumentList(
                                                    TypeArgumentList(
                                                        SeparatedList<TypeSyntax>(new[]
                                                        {
                                                            IdentifierName(context.Compilation
                                                                .GetSemanticModel(f.Finalizer.SyntaxTree)
                                                                .GetDeclaredSymbol(f.Finalizer)!
                                                                .ToDisplayString(SymbolDisplayFormat
                                                                    .FullyQualifiedFormat)),
                                                            IdentifierName(context.Compilation
                                                                .GetSemanticModel(f.Entity.Class.SyntaxTree)
                                                                .GetDeclaredSymbol(f.Entity.Class)!
                                                                .ToDisplayString(SymbolDisplayFormat
                                                                    .FullyQualifiedFormat)),
                                                        })))))
                                    .WithArgumentList(
                                        ArgumentList(
                                            SingletonSeparatedList(
                                                Argument(
                                                    IdentifierName($"{f.Finalizer.Identifier}Identifier")))))))
                            .Append<StatementSyntax>(ReturnStatement(IdentifierName("builder"))))))))
            .NormalizeWhitespace();

        context.AddSource(
            "FinalizerRegistrations.g.cs",
            SourceText.From(declaration.ToString(), Encoding.UTF8, SourceHashAlgorithm.Sha256));
    }

    private static string FinalizerName((ClassDeclarationSyntax Finalizer, AttributedEntity Entity) finalizer)
    {
        var finalizerName = finalizer.Finalizer.Identifier.ToString().ToLowerInvariant();
        var name =
            $"{finalizer.Entity.Group}/{finalizerName}{(finalizerName.EndsWith("finalizer") ? string.Empty : "finalizer")}"
                .TrimStart('/');
        return name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name;
    }
}
