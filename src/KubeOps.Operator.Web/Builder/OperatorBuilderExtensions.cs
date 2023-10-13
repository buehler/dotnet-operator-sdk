using KubeOps.Abstractions.Builder;
using KubeOps.Operator.Web.LocalTunnel;

using Microsoft.Extensions.DependencyInjection;

namespace KubeOps.Operator.Web.Builder;

/// <summary>
/// Method extensions for the operator builder to register web specific services.
/// </summary>
public static class OperatorBuilderExtensions
{
    /// <summary>
    /// Adds a hosted service to the system that creates a "Local Tunnel"
    /// (http://localtunnel.github.io/www/) to the running application.
    /// The tunnel points to the configured host/port configuration and then
    /// registers itself as webhook target within Kubernetes. This
    /// enables developers to easily create webhooks without the requirement
    /// of registering ngrok / localtunnel urls themselves.
    /// </summary>
    /// <param name="builder">The operator builder.</param>
    /// <param name="port">The desired port that the asp.net application will run on.</param>
    /// <param name="hostname">The desired hostname.</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// Attach the development tunnel to the operator if in debug mode.
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services
    ///     .AddKubernetesOperator()
    ///     .RegisterComponents()
    /// #if DEBUG
    ///     .AddDevelopmentTunnel(5000)
    /// #endif
    ///     ;
    /// </code>
    /// </example>
    public static IOperatorBuilder AddDevelopmentTunnel(
        this IOperatorBuilder builder,
        ushort port,
        string hostname = "localhost")
    {
        builder.Services.AddHostedService<DevelopmentTunnelService>();
        builder.Services.AddSingleton(new TunnelConfig(hostname, port));
        return builder;
    }
}
