namespace KubeOps.KubernetesClient.LabelSelectors;

/// <summary>
/// Selector that checks if a certain label does not exist.
/// </summary>
/// <param name="Label">The label that must not exist on the entity/resource.</param>
public record NotExistsSelector(string Label) : LabelSelector
{
    protected override string ToExpression() => $"!{Label}";
}
