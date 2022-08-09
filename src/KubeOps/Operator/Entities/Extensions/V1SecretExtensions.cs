using System.Text;
using k8s.Models;

namespace KubeOps.Operator.Entities.Extensions;

/// <summary>
/// Extensions for <see cref="V1Secret"/>.
/// </summary>
public static class V1SecretExtensions
{
    /// <summary>
    /// Read the data at a given key in a <see cref="V1Secret"/>
    /// and return the <see cref="Encoding.UTF8"/> decoded string.
    /// </summary>
    /// <param name="secret">The secret to read of.</param>
    /// <param name="key">The key for the data value.</param>
    /// <returns>The <see cref="Encoding.UTF8"/> decoded string.</returns>
    public static string ReadData(this V1Secret secret, string key)
        => Encoding.UTF8.GetString(secret.EnsureData()[key]);

    /// <summary>
    /// Write a given string to the <see cref="V1Secret"/>.
    /// The data is <see cref="Encoding.UTF8"/> encoded.
    /// </summary>
    /// <param name="secret">The secret to write to.</param>
    /// <param name="key">The key for the data value.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>The <see cref="V1Secret"/> for chaining.</returns>
    public static V1Secret WriteData(this V1Secret secret, string key, string value)
    {
        secret.EnsureData()[key] = Encoding.UTF8.GetBytes(value);
        return secret;
    }

    private static IDictionary<string, byte[]> EnsureData(this V1Secret secret) =>
        secret.Data ??= new Dictionary<string, byte[]>();
}
