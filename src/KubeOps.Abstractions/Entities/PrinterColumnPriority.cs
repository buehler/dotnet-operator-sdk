namespace KubeOps.Abstractions.Entities;

/// <summary>
/// Specifies the priority of a column in an additional printer view.
/// </summary>
public enum PrinterColumnPriority
{
    /// <summary>
    /// The column is displayed in the standard view.
    /// </summary>
    StandardView,

    /// <summary>
    /// The column is displayed in the wide view.
    /// </summary>
    WideView,
}
