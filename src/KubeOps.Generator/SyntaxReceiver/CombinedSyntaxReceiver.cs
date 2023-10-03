using Microsoft.CodeAnalysis;

namespace KubeOps.Generator.SyntaxReceiver;

internal class CombinedSyntaxReceiver : ISyntaxContextReceiver
{
    private readonly ISyntaxContextReceiver[] _receivers;

    public CombinedSyntaxReceiver(params ISyntaxContextReceiver[] receivers)
    {
        _receivers = receivers;
    }

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        foreach (var syntaxContextReceiver in _receivers)
        {
            syntaxContextReceiver.OnVisitSyntaxNode(context);
        }
    }
}
