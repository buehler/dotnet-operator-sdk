namespace KubeOps.KubernetesClient.LabelSelectors;

/// <summary>
/// Label-selector that checks if a certain label does not contain
/// a specific value (out of a list of values).
/// Note that "label notin (value)" is the same as "label != value".
/// </summary>
/// <param name="Label">The label that must not equal to one of the values.</param>
/// <param name="Values">The possible values.</param>
public record NotEqualsSelector(string Label, params string[] Values) : LabelSelector
{
    protected override string ToExpression() => $"{Label} notin ({string.Join(",", Values)})";
}
