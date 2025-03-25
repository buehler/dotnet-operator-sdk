namespace KubeOps.Abstractions.Kustomize;

public class KustomizationCommonLabelsPair
{
    /// <summary>
    /// A dictionary of common labels.
    /// </summary>
    public IDictionary<string, string>? Pairs { get; set; }

    /// <summary>
    /// Include selectors.
    /// </summary>
    public bool IncludeSelectors { get; init; } = true;
}
