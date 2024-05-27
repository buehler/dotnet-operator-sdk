using System.Text.RegularExpressions;

namespace KubeOps.KubernetesClient;

/// <summary>
/// Method extensions for the <see cref="Exception"/> class.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Walk through all collected Exceptions (base exception and all inner exceptions) LINQ style.
    /// </summary>
    public static IEnumerable<Exception> All(this Exception self)
    {
        if (self == null)
        {
            throw new ArgumentNullException(nameof(self));
        }

        var cause = self;
        do
        {
            yield return cause;
            cause = ReferenceEquals(cause, cause.InnerException) ? null : cause.InnerException;
        }
        while (cause != null && !ReferenceEquals(cause, self));
    }
}
