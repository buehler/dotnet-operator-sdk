namespace KubeOps.Abstractions.Kustomize;

/// <summary>
/// Common labels for the resources.
/// </summary>
public class KustomizationCommonLabels
{
    public KustomizationCommonLabels(IDictionary<string, string> pairs)
    {
        foreach (var keyValuePair in pairs)
        {
            Pairs.Add(keyValuePair.Key, keyValuePair.Value);
        }
    }

    /// <summary>
    /// Include selectors.
    /// </summary>
    public bool IncludeSelectors { get; init; } = true;

    /// <summary>
    /// A list of common labels.
    /// </summary>
    public KustomizationCommonLabelsPair Pairs { get; set; } = new KustomizationCommonLabelsPair();
}
