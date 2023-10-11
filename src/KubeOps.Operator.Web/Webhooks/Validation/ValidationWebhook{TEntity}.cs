using k8s;
using k8s.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KubeOps.Operator.Web.Webhooks.Validation;

/// <summary>
/// The abstract base for any validation webhook. To use them, attach controllers in the
/// main program and map controllers as well. A validation webhook must be decorated
/// with the <see cref="ValidationWebhookAttribute"/> and the type must be provided.
/// There are async and sync methods for each operation. The async will take
/// precedence if both are implemented (i.e. overridden).
/// </summary>
/// <typeparam name="TEntity">The type of the entity that is validated.</typeparam>
/// <example>
/// Simple example of a webhook that checks nothing but is called on every "CREATE" event.
/// <code>
/// [ValidationWebhook(typeof(V1TestEntity))]
/// public class TestValidationWebhook : ValidationWebhook&lt;V1TestEntity&gt;
/// {
///     public override ValidationResult Create(V1TestEntity entity, bool dryRun)
///     {
///         return Success();
///     }
/// }
/// </code>
/// </example>
[ApiController]
public abstract class ValidationWebhook<TEntity> : ControllerBase
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private const string CreateOperation = "CREATE";
    private const string UpdateOperation = "UPDATE";
    private const string DeleteOperation = "DELETE";

    [NonAction]
    public virtual Task<ValidationResult> CreateAsync(TEntity entity, bool dryRun) =>
        Task.FromResult(Create(entity, dryRun));

    [NonAction]
    public virtual ValidationResult Create(TEntity entity, bool dryRun) => Success();

    [NonAction]
    public virtual Task<ValidationResult> UpdateAsync(TEntity oldEntity, TEntity newEntity, bool dryRun) =>
        Task.FromResult(Update(oldEntity, newEntity, dryRun));

    [NonAction]
    public virtual ValidationResult Update(TEntity oldEntity, TEntity newEntity, bool dryRun) => Success();

    [NonAction]
    public virtual Task<ValidationResult> DeleteAsync(TEntity entity, bool dryRun) =>
        Task.FromResult(Delete(entity, dryRun));

    [NonAction]
    public virtual ValidationResult Delete(TEntity entity, bool dryRun) => Success();

    [HttpPost]
    public async Task<IActionResult> Validate([FromBody] AdmissionRequest<TEntity> request)
    {
        var result = request.Request.Operation switch
        {
            CreateOperation => await CreateAsync(request.Request.Object!, request.Request.DryRun),
            UpdateOperation => await UpdateAsync(
                request.Request.OldObject!,
                request.Request.Object!,
                request.Request.DryRun),
            DeleteOperation => await DeleteAsync(request.Request.OldObject!, request.Request.DryRun),
            _ => Fail(
                $"Operation {request.Request.Operation} is not supported.",
                StatusCodes.Status422UnprocessableEntity),
        };

        return result with { Uid = request.Request.Uid };
    }

    [NonAction]
    protected ValidationResult Success(params string[] warnings)
        => new() { Warnings = warnings };

    [NonAction]
    protected ValidationResult Fail()
        => new(false);

    [NonAction]
    protected ValidationResult Fail(string reason)
        => new(false) { Status = new(reason) };

    [NonAction]
    protected ValidationResult Fail(string reason, int statusCode) => new(false) { Status = new(reason, statusCode), };
}
