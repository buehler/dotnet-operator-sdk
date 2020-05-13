using System.Text;
using k8s.Models;

namespace KubeOps.Operator.Entities.Extensions
{
    public static class V1SecretExtensions
    {
        public static string ReadData(this V1Secret secret, string key)
            => Encoding.UTF8.GetString(secret.Data[key]);

        public static void WriteData(this V1Secret secret, string key, string value)
            => secret.Data[key] = Encoding.UTF8.GetBytes(value);
    }
}
