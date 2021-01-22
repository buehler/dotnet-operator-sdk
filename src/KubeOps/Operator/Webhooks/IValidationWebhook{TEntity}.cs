using System;
using System.IO;
using System.Threading.Tasks;
using DotnetKubernetesClient.Entities;
using KubeOps.Operator.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace KubeOps.Operator.Webhooks
{
    /// <summary>
    /// <para>
    /// Validation webhook for kubernetes.
    /// This is used by the https://kubernetes.io/docs/reference/access-authn-authz/extensible-admission-controllers/.
    /// Implement a class with this interface, overwrite the needed functions and register it
    /// to the operator with <see cref="IOperatorBuilder.AddValidationWebhook{TWebhook}"/>.
    /// </para>
    /// <para>
    /// If there are _any_ webhooks registered in the system, the build process
    /// will create the needed certificates for the CA and the service for the
    /// application to run.
    /// </para>
    /// <para>
    /// All methods have a default implementation. They *MUST* return in 10s, otherwise kubernetes will fail the call.
    /// The async methods do call the sync ones and the sync ones
    /// return a "not implemented" result by default.
    /// </para>
    /// <para>
    /// Overwrite the needed ones as defined in <see cref="IValidationWebhook.Operations"/>.
    /// The async implementations take precedence over the synchronous ones.
    /// </para>
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity that should be validated.</typeparam>
    public interface IValidationWebhook<in TEntity> : IValidationWebhook
    {
        /// <summary>
        /// Validation for <see cref="ValidatedOperations.Create"/> operations.
        /// </summary>
        /// <param name="newEntity">The newly created entity that should be validated.</param>
        /// <param name="dryRun">A boolean that indicates if this call was initiated from a dry run (kubectl ... --dry-run).</param>
        /// <returns>A <see cref="ValidationResult"/> that is transmitted to kubernetes.</returns>
        ValidationResult Create(TEntity newEntity, bool dryRun)
            => ValidationResult.NotImplemented();

        /// <summary>
        /// Validation for <see cref="ValidatedOperations.Create"/> operations.
        /// </summary>
        /// <param name="newEntity">The newly created entity that should be validated.</param>
        /// <param name="dryRun">A boolean that indicates if this call was initiated from a dry run (kubectl ... --dry-run).</param>
        /// <returns>A <see cref="ValidationResult"/> that is transmitted to kubernetes.</returns>
        Task<ValidationResult> CreateAsync(TEntity newEntity, bool dryRun)
            => Task.FromResult(Create(newEntity, dryRun));

        /// <summary>
        /// Validation for <see cref="ValidatedOperations.Update"/> operations.
        /// </summary>
        /// <param name="oldEntity">The old entity. This is the "old" version before the update.</param>
        /// <param name="newEntity">The new entity. This is the "new" version after the update is performed.</param>
        /// <param name="dryRun">A boolean that indicates if this call was initiated from a dry run (kubectl ... --dry-run).</param>
        /// <returns>A <see cref="ValidationResult"/> that is transmitted to kubernetes.</returns>
        ValidationResult Update(TEntity oldEntity, TEntity newEntity, bool dryRun)
            => ValidationResult.NotImplemented();

        /// <summary>
        /// Validation for <see cref="ValidatedOperations.Update"/> operations.
        /// </summary>
        /// <param name="oldEntity">The old entity. This is the "old" version before the update.</param>
        /// <param name="newEntity">The new entity. This is the "new" version after the update is performed.</param>
        /// <param name="dryRun">A boolean that indicates if this call was initiated from a dry run (kubectl ... --dry-run).</param>
        /// <returns>A <see cref="ValidationResult"/> that is transmitted to kubernetes.</returns>
        Task<ValidationResult> UpdateAsync(TEntity oldEntity, TEntity newEntity, bool dryRun)
            => Task.FromResult(Update(oldEntity, newEntity, dryRun));

        /// <summary>
        /// Validation for <see cref="ValidatedOperations.Delete"/> operations.
        /// </summary>
        /// <param name="oldEntity">The entity that is being deleted.</param>
        /// <param name="dryRun">A boolean that indicates if this call was initiated from a dry run (kubectl ... --dry-run).</param>
        /// <returns>A <see cref="ValidationResult"/> that is transmitted to kubernetes.</returns>
        ValidationResult Delete(TEntity oldEntity, bool dryRun)
            => ValidationResult.NotImplemented();

        /// <summary>
        /// Validation for <see cref="ValidatedOperations.Delete"/> operations.
        /// </summary>
        /// <param name="oldEntity">The entity that is being deleted.</param>
        /// <param name="dryRun">A boolean that indicates if this call was initiated from a dry run (kubectl ... --dry-run).</param>
        /// <returns>A <see cref="ValidationResult"/> that is transmitted to kubernetes.</returns>
        Task<ValidationResult> DeleteAsync(TEntity oldEntity, bool dryRun)
            => Task.FromResult(Delete(oldEntity, dryRun));

        internal void Register(IEndpointRouteBuilder endpoints)
        {
            var crd = typeof(TEntity).CreateResourceDefinition();
            var endpoint = $"/{crd.Group}/{crd.Version}/{crd.Plural}/validate".ToLowerInvariant();

            endpoints.MapPost(
                endpoint,
                async context =>
                {
                    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

                    if (!context.Request.HasJsonContentType())
                    {
                        logger.LogError("Validation request has no json content type.");
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        return;
                    }

                    using var reader = new StreamReader(context.Request.Body);
                    var review = JsonConvert.DeserializeObject<AdmissionReview<TEntity>>(await reader.ReadToEndAsync());

                    if (review.Request == null)
                    {
                        logger.LogError("The validation request contained no request object.");
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("The request must contain AdmissionRequest information.");
                        return;
                    }

                    AdmissionReview<TEntity> result;
                    try
                    {
                        if (!(context.RequestServices.GetRequiredService(GetType()) is IValidationWebhook<TEntity>
                            validator))
                        {
                            throw new Exception("Validator is not a valid IValidationWebhook<TEntity>");
                        }

                        logger.LogDebug(@"Validate with method ""{method}"".", review.Request.Operation);
                        var response = review.Request.Operation switch
                        {
                            "CREATE" => await validator.CreateAsync(
                                review.Request.Object ?? throw new Exception("Object is null in create validation"),
                                review.Request.DryRun),
                            "UPDATE" => await validator.UpdateAsync(
                                review.Request.OldObject ??
                                throw new Exception("OldObject is null in create validation"),
                                review.Request.Object ?? throw new Exception("Object is null in create validation"),
                                review.Request.DryRun),
                            "DELETE" => await validator.DeleteAsync(
                                review.Request.OldObject ??
                                throw new Exception("OldObject is null in create validation"),
                                review.Request.DryRun),
                            _ => throw new NotImplementedException(
                                $@"Operation ""{review.Request.Operation}"" not implemented."),
                        };

                        result = new AdmissionReview<TEntity>(response.CreateResponse(review.Request.Uid));
                        logger.LogInformation(
                            @"Validator ""{name}"" did return ""{result}"" for ""{operation}"".",
                            GetType().Name,
                            result.Response?.Allowed,
                            review.Request.Operation);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error happened during validation.");
                        result = new AdmissionReview<TEntity>(
                            ValidationResult
                                .Fail(
                                    StatusCodes.Status500InternalServerError,
                                    "There was an internal server error.")
                                .CreateResponse(review.Request.Uid));
                    }

                    await context.Response.WriteAsJsonAsync(result);
                });
        }
    }
}
