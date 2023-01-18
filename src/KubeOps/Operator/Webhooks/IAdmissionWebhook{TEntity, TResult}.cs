using k8s;
using KubeOps.Operator.Entities.Extensions;

namespace KubeOps.Operator.Webhooks;

/// <summary>
/// Base interface for all admission webhooks.
/// </summary>
/// <typeparam name="TEntity">Type of the entity that is managed.</typeparam>
/// <typeparam name="TResult">Result type of the webhook.</typeparam>
public interface IAdmissionWebhook<TEntity, TResult>
    where TResult : AdmissionResult, new()
{
    /// <summary>
    /// The operations that the webhook wants to be notified about.
    /// All subscribed events are forwarded to the validation webhook.
    /// </summary>
    AdmissionOperations Operations { get; }

    internal string Name =>
        $"{GetType().Namespace ?? "root"}.{typeof(TEntity).Name}.{GetType().Name}".ToLowerInvariant();

    internal string Endpoint { get; }

    internal IList<string> SupportedOperations
    {
        get
        {
            if (Operations.HasFlag(AdmissionOperations.All))
            {
                return new List<string> { "*" };
            }

            var result = new List<string>();

            if (Operations.HasFlag(AdmissionOperations.Create))
            {
                result.Add("CREATE");
            }

            if (Operations.HasFlag(AdmissionOperations.Update))
            {
                result.Add("UPDATE");
            }

            if (Operations.HasFlag(AdmissionOperations.Delete))
            {
                result.Add("DELETE");
            }

            return result;
        }
    }

    /// <summary>
    /// Operation for <see cref="AdmissionOperations.Create"/>.
    /// </summary>
    /// <param name="newEntity">The newly created entity that should be validated.</param>
    /// <param name="dryRun">A boolean that indicates if this call was initiated from a dry run (kubectl ... --dry-run).</param>
    /// <returns>A result that is transmitted to kubernetes.</returns>
    TResult Create(TEntity newEntity, bool dryRun);

    /// <inheritdoc cref="Create"/>
    Task<TResult> CreateAsync(TEntity newEntity, bool dryRun)
        => Task.FromResult(Create(newEntity, dryRun));

    /// <summary>
    /// Operation for <see cref="AdmissionOperations.Update"/>.
    /// </summary>
    /// <param name="oldEntity">The old entity. This is the "old" version before the update.</param>
    /// <param name="newEntity">The new entity. This is the "new" version after the update is performed.</param>
    /// <param name="dryRun">A boolean that indicates if this call was initiated from a dry run (kubectl ... --dry-run).</param>
    /// <returns>A result that is transmitted to kubernetes.</returns>
    TResult Update(TEntity oldEntity, TEntity newEntity, bool dryRun);

    /// <inheritdoc cref="Update"/>
    Task<TResult> UpdateAsync(TEntity oldEntity, TEntity newEntity, bool dryRun)
        => Task.FromResult(Update(oldEntity, newEntity, dryRun));

    /// <summary>
    /// Operation for <see cref="AdmissionOperations.Delete"/>.
    /// </summary>
    /// <param name="oldEntity">The entity that is being deleted.</param>
    /// <param name="dryRun">A boolean that indicates if this call was initiated from a dry run (kubectl ... --dry-run).</param>
    /// <returns>A result that is transmitted to kubernetes.</returns>
    TResult Delete(TEntity oldEntity, bool dryRun);

    /// <inheritdoc cref="Delete"/>
    Task<TResult> DeleteAsync(TEntity oldEntity, bool dryRun)
        => Task.FromResult(Delete(oldEntity, dryRun));

    internal AdmissionResponse TransformResult(
        TResult result,
        AdmissionRequest<TEntity> request);

    internal void Register(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
            Endpoint,
            async context =>
            {
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());

                if (!context.Request.HasJsonContentType())
                {
                    logger.LogError("Admission request has no json content type.");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                var review = KubernetesJson.Deserialize<AdmissionReview<TEntity>>(context.Request.Body.ToString());

                if (review.Request == null)
                {
                    logger.LogError("The admission request contained no request object.");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("The request must contain AdmissionRequest information.");
                    return;
                }

                AdmissionResponse response;
                try
                {
                    using var scope = context.RequestServices.CreateScope();
                    if (scope.ServiceProvider.GetRequiredService(GetType()) is not
                        IAdmissionWebhook<TEntity, TResult>
                        webhook)
                    {
                        throw new Exception("Object is not a valid IAdmissionWebhook<TEntity, TResult>");
                    }

                    var @object = review.Request.Object.DeepClone();
                    var oldObject = review.Request.OldObject.DeepClone();

                    logger.LogDebug(@"Admission with method ""{method}"".", review.Request.Operation);
                    var result = review.Request.Operation switch
                    {
                        "CREATE" => await webhook.CreateAsync(
                            @object ?? throw new Exception("Object is null in create admission"),
                            review.Request.DryRun),
                        "UPDATE" => await webhook.UpdateAsync(
                            oldObject ??
                            throw new Exception("OldObject is null in create admission"),
                            @object ?? throw new Exception("Object is null in update admission"),
                            review.Request.DryRun),
                        "DELETE" => await webhook.DeleteAsync(
                            oldObject ??
                            throw new Exception("OldObject is null in delete admission"),
                            review.Request.DryRun),
                        _ => throw new NotImplementedException(
                            $@"Operation ""{review.Request.Operation}"" not implemented."),
                    };

                    response = TransformResult(result, review.Request);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error happened during admission.");
                    response = AdmissionResult.InternalServerError();
                }

                review.Response = response;
                review.Response.Uid = review.Request.Uid;

                logger.LogInformation(
                    @"AdmissionHook ""{name}"" did return ""{result}"" for ""{operation}"".",
                    Name,
                    review.Response?.Allowed,
                    review.Request.Operation);

                review.Request = null;

                await context.Response.WriteAsJsonAsync(review);
            });
}
