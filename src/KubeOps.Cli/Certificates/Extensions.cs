using System.Text;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

namespace KubeOps.Cli.Certificates;

internal static class Extensions
{
    public static string ToPem(this X509Certificate cert) => ObjToPem(cert);

    public static string ToPem(this AsymmetricCipherKeyPair key) => ObjToPem(key);

    private static string ObjToPem(object obj)
    {
        var sb = new StringBuilder();
        using var writer = new PemWriter(new StringWriter(sb));
        writer.WriteObject(obj);
        return sb.ToString();
    }
}
