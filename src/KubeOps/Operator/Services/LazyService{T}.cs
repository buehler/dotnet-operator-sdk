using System;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Services
{
    internal class LazyService<T> : Lazy<T>
        where T : class
    {
        public LazyService(IServiceProvider provider)
            : base(provider.GetRequiredService<T>)
        {
        }
    }
}
