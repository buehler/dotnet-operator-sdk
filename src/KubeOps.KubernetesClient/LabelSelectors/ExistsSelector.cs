namespace KubeOps.KubernetesClient.LabelSelectors;

/// <summary>
/// Selector that checks if a certain label exists.
/// </summary>
/// <param name="Label">The label that needs to exist on the entity/resource.</param>
public record ExistsSelector(string Label) : LabelSelector
{
    protected override string ToExpression() => $"{Label}";
}
