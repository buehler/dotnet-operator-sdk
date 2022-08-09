namespace KubeOps.Operator.Util;

internal static class StringExtensions
{
    private const byte MaxNameLength = 254;

    internal static string ToCamelCase(this string value) =>
        string.IsNullOrWhiteSpace(value) || char.IsLower(value[0])
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];

    internal static string TrimWebhookName(this string name, string prefix = "")
    {
        var tmp = prefix + name;
        tmp = tmp.Length < MaxNameLength ? tmp : tmp[..MaxNameLength];
        return tmp.ToLowerInvariant();
    }

    internal static string FormatWebhookUrl(this string baseUrl, string endpoint)
    {
        if (!baseUrl.StartsWith("https://"))
        {
            throw new ArgumentException(@"The base url must start with ""https://"".");
        }

        return baseUrl.Trim().TrimEnd('/') + endpoint;
    }
}
