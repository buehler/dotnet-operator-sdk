using Microsoft.CodeAnalysis;

namespace KubeOps.Generator.SyntaxReceiver;

internal class CombinedSyntaxReceiver(params ISyntaxContextReceiver[] receivers) : ISyntaxContextReceiver
{
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        foreach (var syntaxContextReceiver in receivers)
        {
            syntaxContextReceiver.OnVisitSyntaxNode(context);
        }
    }
}
