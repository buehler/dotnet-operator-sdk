namespace KubeOps.Operator.Entities.Annotations;

/// <summary>
/// Defines a property as an additional printer column.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AdditionalPrinterColumnAttribute : Attribute
{
    /// <summary>
    /// The name of the column. Defaults to the property-name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The priority of the additional printer column.
    /// As documented in
    /// <a href="https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definitions/#priority">https://kubernetes.io/docs/tasks/extend-kubernetes/custom-resources/custom-resource-definitions/#priority</a>
    /// the following rules apply to priority:
    /// <list type="bullet">
    /// <item>
    /// <description>Columns with priority `0` are shown in standard view</description>
    /// </item>
    /// <item>
    /// <description>Columns with priority greater than `0` are shown only in wide view</description>
    /// </item>
    /// </list>
    /// </summary>
    public int Priority { get; set; }
}
