using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace KubeOps.Operator.Logging
{
    /// <summary>
    /// Extensions for the <see cref="ILoggingBuilder"/>.
    /// </summary>
    public static class LoggingBuilderExtensions
    {
        /// <summary>
        /// Define a structured logger on the logging builder.
        /// The structured logger creates json output with classical
        /// javascript json notation.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/>.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> for chaining.</returns>
        public static ILoggingBuilder AddStructuredConsole(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(
                ServiceDescriptor
                    .Singleton<ILoggerProvider, StructuredConsoleLoggerProvider>());

            return builder;
        }
    }
}
