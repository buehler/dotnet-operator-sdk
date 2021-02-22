namespace KubeOps.Operator.Util
{
    internal static class StringExtensions
    {
        internal static string ToCamelCase(this string value) =>
            string.IsNullOrWhiteSpace(value) || char.IsLower(value[0])
                ? value
                : char.ToLowerInvariant(value[0]) + value[1..];
    }
}
