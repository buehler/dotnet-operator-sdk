using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotnetKubernetesClient.Entities;
using KubeOps.Operator.Builder;
using KubeOps.Operator.Entities.Extensions;
using KubeOps.Operator.Webhooks;
using KubeOps.Operator.Webhooks.ConversionWebhook;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace KubeOps.Operator;

/// <summary>
/// Extensions for the <see cref="IApplicationBuilder"/>.
/// </summary>
public static class ApplicationBuilderExtensions
{
    private const string ApiVersion = "apiextensions.k8s.io/v1";
    private const string Kind = "ConversionReview";

    /// <summary>
    /// Use the kubernetes operator.
    /// Register routing (.UseRouting()) and endpoints.
    /// The endpoints contain health-checks and metrics endpoints.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    public static void UseKubernetesOperator(
        this IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(
            endpoints =>
            {
                var settings = app.ApplicationServices.GetRequiredService<OperatorSettings>();

                endpoints.MapHealthChecks(
                    settings.LivenessEndpoint,
                    new HealthCheckOptions { Predicate = reg => reg.Tags.Contains(OperatorBuilder.LivenessTag) });
                endpoints.MapHealthChecks(
                    settings.ReadinessEndpoint,
                    new HealthCheckOptions { Predicate = reg => reg.Tags.Contains(OperatorBuilder.ReadinessTag) });

                endpoints.MapMetrics(settings.MetricsEndpoint);

                var logger = app.ApplicationServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("ApplicationStartup");

                using var scope = app.ApplicationServices.CreateScope();
                var componentRegistrar = scope.ServiceProvider.GetRequiredService<IComponentRegistrar>();
                var webhookMetadataBuilder = scope.ServiceProvider.GetRequiredService<IWebhookMetadataBuilder>();

                foreach (var wh in componentRegistrar.ValidatorRegistrations)
                {
                    (Type validatorType, Type entityType) = wh;

                    var validator = scope.ServiceProvider.GetRequiredService(validatorType);
                    var registerMethod = typeof(IAdmissionWebhook<,>)
                        .MakeGenericType(entityType, typeof(ValidationResult))
                        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                        .First(m => m.Name == "Register");
                    registerMethod.Invoke(validator, new object[] { endpoints });
                    var (name, endpoint) =
                        webhookMetadataBuilder.GetMetadata<ValidationResult>(validator, entityType);
                    logger.LogInformation(
                        @"Registered validation webhook ""{name}"" under ""{endpoint}"".",
                        name,
                        endpoint);
                }

                foreach (var wh in componentRegistrar.MutatorRegistrations)
                {
                    (Type mutatorType, Type entityType) = wh;

                    var mutator = scope.ServiceProvider.GetRequiredService(mutatorType);
                    var registerMethod = typeof(IAdmissionWebhook<,>)
                        .MakeGenericType(entityType, typeof(MutationResult))
                        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                        .First(m => m.Name == "Register");
                    registerMethod.Invoke(mutator, new object[] { endpoints });
                    var (name, endpoint) =
                        webhookMetadataBuilder.GetMetadata<MutationResult>(mutator, entityType);
                    logger.LogInformation(
                        @"Registered mutation webhook ""{name}"" under ""{endpoint}"".",
                        name,
                        endpoint);
                }

                RegisterConversionWebhooks(app, logger, componentRegistrar);
            });
    }

    private static void RegisterConversionWebhooks(IApplicationBuilder app, ILogger logger, IComponentRegistrar componentRegistrar)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapPost(
                "/convert",
                async context =>
                {
                    using var postScope = app.ApplicationServices.CreateScope();
                    if (!context.Request.HasJsonContentType())
                    {
                        logger.LogError("Admission request has no json content type");
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        return;
                    }

                    Response response;
                    Guid requestUid = Guid.Empty;
                    try
                    {
                        using var reader = new StreamReader(context.Request.Body);
                        var jsonContent = await reader.ReadToEndAsync();
                        var desiredApiVersion = JsonNode.Parse(jsonContent)!["request"]!["desiredAPIVersion"]!
                            .GetValue<string>();
                        requestUid = JsonNode.Parse(jsonContent)!["request"]!["uid"]!.GetValue<Guid>();
                        var objectsToConvert = JsonNode.Parse(jsonContent)!["request"]!["objects"]!.AsArray();
                        logger.LogInformation("Conversion request with id: {Id} desiredApiVersion: {Version}, AmountOfObjectsToConvert: {Amount}", requestUid.ToString(), desiredApiVersion, objectsToConvert.Count);
                        var convertedObjects = new List<object>();
                        foreach (var objectToConvert in objectsToConvert)
                        {
                            var kind = objectToConvert!["kind"]!.GetValue<string>();
                            var currentApiVersion = objectToConvert!["apiVersion"]!.GetValue<string>();
                            (Type webhookTypeToCall, Type entityIn, Type entityOut) =
                                componentRegistrar.ConversionRegistrations.First(
                                    cw =>
                                        cw.EntityIn.CreateResourceDefinition().Kind == kind &&
                                        cw.EntityIn.CreateResourceDefinition().GroupVersion() == currentApiVersion &&
                                        cw.EntityOut.CreateResourceDefinition().GroupVersion() == desiredApiVersion);
                            var input = objectToConvert.Deserialize(
                                entityIn,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            var conversionResult = ConvertCustomResource(
                                postScope,
                                webhookTypeToCall,
                                entityIn,
                                entityOut,
                                input,
                                logger);
                            convertedObjects.Add(conversionResult ?? throw new InvalidOperationException());
                        }

                        response = new Response(requestUid, Result.Success(), convertedObjects);
                    }
                    catch (Exception e)
                    {
                        logger.LogError("Error while converting objects: {Exception}", e.Message);
                        response = new Response(requestUid, Result.Failed("failed converting resources"), null);
                    }

                    await context.Response.WriteAsJsonAsync(new ConversionResponse(ApiVersion, Kind, response));
                });
        });
    }

    private static object? ConvertCustomResource(IServiceScope postScope, Type webhookTypeToCall, Type entityIn, Type entityOut, object? input, ILogger logger)
    {
        var mutator = postScope.ServiceProvider.GetRequiredService(webhookTypeToCall);
        var genericConvertor = typeof(IConversionWebhook<,>)
            .MakeGenericType(entityIn, entityOut);
        var convertMethod = genericConvertor
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .First(m => m.Name == "Convert");
        logger.LogDebug("Invoking method for conversion: {Method}", convertMethod.Name);
        return convertMethod.Invoke(
            mutator,
            new[] { input ?? throw new InvalidOperationException() });
    }
}
