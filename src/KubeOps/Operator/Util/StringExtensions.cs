namespace KubeOps.Operator.Util
{
    internal static class StringExtensions
    {
        internal static string ToCamelCase(this string value) =>
            string.IsNullOrWhiteSpace(value)
                ? value
                : char.ToLowerInvariant(value[0]) + value[1..];
    }
}
