using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dos.Operator.DependencyInjection
{
    internal static class DependencyInjector
    {
        public static IServiceProvider Services { get; set; } = new ServiceCollection().BuildServiceProvider();
    }
}
