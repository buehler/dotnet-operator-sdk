using System.Text.RegularExpressions;

namespace KubeOps.Transpiler.Kubernetes;

/// <summary>
/// KubernetesVersionComparer. TODO.
/// </summary>
public sealed partial class KubernetesVersionComparer : IComparer<string>
{
#if !NET7_0_OR_GREATER
    private static readonly Regex KubernetesVersionRegex =
        new("^v(?<major>[0-9]+)((?<stream>alpha|beta)(?<minor>[0-9]+))?$", RegexOptions.Compiled);
#endif

    private enum Stream
    {
        Alpha = 1,
        Beta = 2,
        Final = 3,
    }

    public int Compare(string? x, string? y)
    {
        if (x == null || y == null)
        {
            return StringComparer.CurrentCulture.Compare(x, y);
        }

#if NET7_0_OR_GREATER
        var matchX = KubernetesVersionRegex().Match(x);
#else
        var matchX = KubernetesVersionRegex.Match(x);
#endif
        if (!matchX.Success)
        {
            return StringComparer.CurrentCulture.Compare(x, y);
        }

#if NET7_0_OR_GREATER
        var matchY = KubernetesVersionRegex().Match(y);
#else
        var matchY = KubernetesVersionRegex.Match(y);
#endif
        if (!matchY.Success)
        {
            return StringComparer.CurrentCulture.Compare(x, y);
        }

        var versionX = ExtractVersion(matchX);
        var versionY = ExtractVersion(matchY);
        return versionX.CompareTo(versionY);
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex("^v(?<major>[0-9]+)((?<stream>alpha|beta)(?<minor>[0-9]+))?$", RegexOptions.Compiled)]
    private static partial Regex KubernetesVersionRegex();
#endif

    private Version ExtractVersion(Match match)
    {
        var major = int.Parse(match.Groups["major"].Value);
        if (!Enum.TryParse<Stream>(match.Groups["stream"].Value, true, out var stream))
        {
            stream = Stream.Final;
        }

        _ = int.TryParse(match.Groups["minor"].Value, out var minor);
        return new Version(major, (int)stream, minor);
    }
}
