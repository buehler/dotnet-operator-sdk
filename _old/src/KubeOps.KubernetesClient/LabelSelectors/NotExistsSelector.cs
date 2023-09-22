namespace KubeOps.KubernetesClient.LabelSelectors;

/// <summary>
/// Selector that checks if a certain label does not exist.
/// </summary>
public record NotExistsSelector(string Label) : ILabelSelector
{
    public string ToExpression() => $"!{Label}";
}
