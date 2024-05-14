using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KubeOps.Abstractions.Certificates
{
    /// <summary>
    /// Defines properties for certificate/key pair so a custom certificate/key provider may be implemented.
    /// The provider is used by the CertificateWebhookService to provide a caBundle to the webhooks.
    /// </summary>
    public interface ICertificateProvider : IDisposable
    {
        /// <summary>
        /// The server certificate and key.
        /// </summary>
        CertificatePair Server { get; }

        /// <summary>
        /// The root certificate and key.
        /// </summary>
        CertificatePair Root { get; }
    }
}
