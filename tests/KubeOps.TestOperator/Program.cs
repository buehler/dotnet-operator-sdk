using System.Threading.Tasks;

namespace KubeOps.TestOperator
{
    public static class Program
    {
        public static Task<int> Main(string[] args) => new Operator().Run(args);
    }
}
