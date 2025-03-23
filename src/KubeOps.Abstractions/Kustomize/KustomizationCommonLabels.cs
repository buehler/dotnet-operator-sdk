namespace KubeOps.Abstractions.Kustomize;

/// <summary>
/// Common labels for the resources.
/// </summary>
public class KustomizationCommonLabels
{
    public KustomizationCommonLabels(IDictionary<string, string> pairs)
    {
        Pairs.Add(new KustomizationCommonLabelsPair() { Pairs = pairs });
    }

    /// <summary>
    /// A list of common labels.
    /// </summary>
    public List<KustomizationCommonLabelsPair> Pairs { get; set; } = new List<KustomizationCommonLabelsPair>();
}
