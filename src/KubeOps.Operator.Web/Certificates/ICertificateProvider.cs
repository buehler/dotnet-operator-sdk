using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KubeOps.Operator.Web.Certificates
{
    /// <summary>
    /// Defines properties for certificates and keys so a custom certificate/key provider may be implemented.
    /// The provider is used by the <see cref="CertificateWebhookService"/> to provide a caBundle to the webhooks.
    /// </summary>
    public interface ICertificateProvider : IDisposable
    {
        /// <summary>
        /// The server certificate and key.
        /// </summary>
        (X509Certificate2 Certificate, AsymmetricAlgorithm Key) Server { get; }

        /// <summary>
        /// The root certificate and key.
        /// </summary>
        (X509Certificate2 Certificate, AsymmetricAlgorithm Key) Root { get; }
    }
}
