namespace KubeOps.KubernetesClient.LabelSelectors;

public static class Extensions
{
    /// <summary>
    /// Convert an enumerable list of <see cref="ILabelSelector"/>s to a string.
    /// </summary>
    /// <param name="selectors">The list of selectors.</param>
    /// <returns>A comma-joined string with all selectors converted to their expressions.</returns>
    public static string ToExpression(this IEnumerable<ILabelSelector> selectors) =>
        string.Join(",", selectors.Select(s => s.ToExpression()));
}
