using KubeOps.Operator.Commands;
using McMaster.Extensions.CommandLineUtils;

namespace KubeOps.Operator;

/// <summary>
/// Extensions for the <see cref="IHost"/>.
/// </summary>
public static class HostExtensions
{
    /// <summary>
    /// Run the operator with default settings.
    /// Creates the application, creates the constructor-injection
    /// and runs the application with the given arguments.
    /// </summary>
    /// <param name="host">The <see cref="IHost"/>.</param>
    /// <param name="args">Program arguments.</param>
    /// <returns>Async task with completion result.</returns>
    public static Task<int> RunOperatorAsync(this IHost host, string[] args)
    {
        var app = new CommandLineApplication<RunOperator>();
        app
            .Conventions
            .UseDefaultConventions()
            .UseConstructorInjection(host.Services);

        return app.ExecuteAsync(args);
    }
}
