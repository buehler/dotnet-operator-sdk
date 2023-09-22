namespace KubeOps.KubernetesClient.LabelSelectors;

/// <summary>
/// Selector that checks if a certain label exists.
/// </summary>
public record ExistsSelector(string Label) : ILabelSelector
{
    public string ToExpression() => $"{Label}";
}
