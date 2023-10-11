using k8s;
using k8s.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KubeOps.Operator.Web.Webhooks.Validation;

[ApiController]
public class ValidationWebhook<TEntity> : ControllerBase
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private const string CreateOperation = "CREATE";
    private const string UpdateOperation = "UPDATE";
    private const string DeleteOperation = "DELETE";

    [NonAction]
    public virtual Task<ValidationResult> Create(TEntity entity, bool dryRun) => Task.FromResult(Success());

    [NonAction]
    public virtual Task<ValidationResult> Update(TEntity oldEntity, TEntity newEntity, bool dryRun) =>
        Task.FromResult(Success());

    [NonAction]
    public virtual Task<ValidationResult> Delete(TEntity entity, bool dryRun) => Task.FromResult(Success());

    [HttpPost]
    public async Task<IActionResult> Validate([FromBody] AdmissionRequest<TEntity> request)
    {
        var result = request.Request.Operation switch
        {
            CreateOperation => await Create(request.Request.Object!, request.Request.DryRun),
            UpdateOperation => await Update(
                request.Request.OldObject!,
                request.Request.Object!,
                request.Request.DryRun),
            DeleteOperation => await Delete(request.Request.OldObject!, request.Request.DryRun),
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
