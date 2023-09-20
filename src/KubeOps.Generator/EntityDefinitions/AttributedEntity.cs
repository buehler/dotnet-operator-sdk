using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace KubeOps.Generator.EntityDefinitions;

public record struct AttributedEntity(
    ClassDeclarationSyntax Class,
    string Kind,
    string Version,
    string? Group,
    string? Plural);
