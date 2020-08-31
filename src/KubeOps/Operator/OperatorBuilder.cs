using k8s;
using k8s.Models;
using KubeOps.Operator.Controller;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator
{
    public class OperatorBuilder : IOperatorBuilder
    {
        private readonly IServiceCollection _services;

        public OperatorBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public OperatorBuilder AddController<T>()
            where T : class, IResourceController
        {
            _services.AddResourceController<T>();
            return this;
        }
    }
}
