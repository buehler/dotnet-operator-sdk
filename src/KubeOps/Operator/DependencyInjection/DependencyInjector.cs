using System;
using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.DependencyInjection
{
    internal static class DependencyInjector
    {
        public static IServiceProvider Services { get; set; } = new ServiceCollection().BuildServiceProvider();
    }
}
