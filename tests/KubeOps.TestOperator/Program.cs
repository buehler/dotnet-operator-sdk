using System.Threading.Tasks;
using KubeOps.Operator;

namespace KubeOps.TestOperator
{
    public static class Program
    {
        public static Task<int> Main(string[] args) => new KubernetesOperator<Startup>().Run(args);
    }
}
