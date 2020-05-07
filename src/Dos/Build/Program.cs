using System.Threading.Tasks;
using Dos.Operator;

namespace Dos.Build
{
    internal static class Program
    {
        public static Task Main(string[] args) => new KubernetesOperator().Run(args);
    }
}
