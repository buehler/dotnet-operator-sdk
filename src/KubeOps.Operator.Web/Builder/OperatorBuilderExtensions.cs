using System.Reflection;
using System.Runtime.Versioning;

using KubeOps.Abstractions.Builder;
using KubeOps.Operator.Web.Certificates;
using KubeOps.Operator.Web.LocalTunnel;
using KubeOps.Operator.Web.Webhooks;

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
    [RequiresPreviewFeatures(
        "Localtunnel is sometimes unstable, use with caution. " +
        "This API is in preview and may be removed in future versions if no stable alternative is found.")]
    public static IOperatorBuilder AddDevelopmentTunnel(
        this IOperatorBuilder builder,
        ushort port,
        string hostname = "localhost")
    {
        builder.Services.AddHostedService<TunnelWebhookService>();
        builder.Services.AddSingleton(new WebhookLoader(Assembly.GetEntryAssembly()!));
        builder.Services.AddSingleton(new WebhookConfig(hostname, port));
        builder.Services.AddSingleton<DevelopmentTunnel>();

        return builder;
    }

    /// <summary>
    /// Adds a hosted service to the system that uses the server certificate from an <see cref="ICertificateProvider"/>
    /// implementation to configure development webhooks without tunnels. The webhooks will be configured to use the hostname and port.
    /// </summary>
    /// <param name="builder">The operator builder.</param>
    /// <param name="port">The port that the webhooks will use to connect to the operator.</param>
    /// <param name="hostname">The hostname, IP, or FQDN of the machine running the operator.</param>
    /// <param name="certificateProvider">The <see cref="ICertificateProvider"/> the <see cref="CertificateWebhookService"/>
    /// will use to generate the PEM-encoded server certificate for the webhooks.</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// Use the development webhooks.
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// string ip = "192.168.1.100";
    /// ushort port = 443;
    ///
    /// using CertificateGenerator generator = new CertificateGenerator(ip);
    /// using X509Certificate2 cert = generator.Server.CopyServerCertWithPrivateKey();
    /// // Configure Kestrel to listen on IPv4, use port 443, and use the server certificate
    /// builder.WebHost.ConfigureKestrel(serverOptions =>
    /// {
    ///     serverOptions.Listen(System.Net.IPAddress.Any, port, async listenOptions =>
    ///     {
    ///         listenOptions.UseHttps(cert);
    ///     });
    /// });
    ///  builder.Services
    ///      .AddKubernetesOperator()
    ///      // Create the development webhook service using the cert provider
    ///      .UseCertificateProvider(port, ip, generator)
    ///      // More code
    ///
    /// </code>
    /// </example>
    public static IOperatorBuilder UseCertificateProvider(this IOperatorBuilder builder, ushort port, string hostname, ICertificateProvider certificateProvider)
    {
        builder.Services.AddHostedService<CertificateWebhookService>();
        builder.Services.AddSingleton(new WebhookLoader(Assembly.GetEntryAssembly()!));
        builder.Services.AddSingleton(new WebhookConfig(hostname, port));
        builder.Services.AddSingleton(certificateProvider);

        return builder;
    }
}
