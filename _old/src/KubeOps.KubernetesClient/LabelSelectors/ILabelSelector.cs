namespace KubeOps.KubernetesClient.LabelSelectors;

/// <summary>
/// Different label selectors for querying the Kubernetes API.
/// </summary>
public interface ILabelSelector
{
    /// <summary>
    /// Create an expression from the label selector.
    /// </summary>
    /// <returns>A string that represents the label selector.</returns>
    string ToExpression();
}
