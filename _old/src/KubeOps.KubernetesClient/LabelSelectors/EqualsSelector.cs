namespace KubeOps.KubernetesClient.LabelSelectors;

/// <summary>
/// Label-selector that checks if a certain label contains
/// a specific value (out of a list of values).
/// Note that "label in (value)" is the same as "label == value".
/// </summary>
public record EqualsSelector : ILabelSelector
{
    public EqualsSelector(string label, params string[] values) => (Label, Values) = (label, values);

    public string Label { get; }

    public IEnumerable<string> Values { get; }

    public string ToExpression() => $"{Label} in ({string.Join(",", Values)})";
}
