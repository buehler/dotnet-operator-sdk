using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace KubeOps.Operator.Logging
{
    public static class LoggingBuilderExtensions
    {
        public static ILoggingBuilder AddStructuredConsole(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(ServiceDescriptor
                .Singleton<ILoggerProvider, StructuredConsoleLoggerProvider>());

            return builder;
        }
    }
}
