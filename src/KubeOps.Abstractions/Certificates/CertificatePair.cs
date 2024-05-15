using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KubeOps.Abstractions.Certificates
{
    public record CertificatePair(X509Certificate2 Certificate, AsymmetricAlgorithm Key);
}
