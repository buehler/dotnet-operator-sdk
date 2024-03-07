using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KubeOps.Generator.Test;

internal static class TestHelperExtensions
{
    public static Compilation CreateCompilation(this string source)
    {
        var compilation = CSharpCompilation.Create(
            "compilation",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation;
    }
}
