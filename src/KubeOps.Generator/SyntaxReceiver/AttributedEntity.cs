using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KubeOps.Generator.SyntaxReceiver;

internal record struct AttributedEntity(
    ClassDeclarationSyntax Class,
    string Kind,
    string Version,
    string? Group,
    string? Plural);
