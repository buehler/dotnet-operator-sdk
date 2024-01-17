using System.Text.Json.Nodes;

using k8s;
using k8s.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KubeOps.Operator.Web.Webhooks.Mutation;

/// <summary>
/// The mutation result for the mutation (admission) request to a webhook.
/// </summary>
/// <param name="ModifiedObject">The modified entity if any changes are requested.</param>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public record MutationResult<TEntity>(TEntity? ModifiedObject = default) : IActionResult
    where TEntity : IKubernetesObject<V1ObjectMeta>
{
    private const string JsonPatch = "JSONPatch";

    internal string Uid { get; init; } = string.Empty;

    internal JsonNode? OriginalObject { get; init; }

    public bool Valid { get; init; } = true;

    /// <summary>
    /// Provides additional information to the validation result.
    /// The message is displayed to the user if the validation fails.
    /// The status code can provide more information about the error.
    /// <a href="https://kubernetes.io/docs/reference/access-authn-authz/extensible-admission-controllers/#response">
    /// See "Extensible Admission Controller Response"
    /// </a>
    /// </summary>
    public AdmissionStatus? Status { get; init; }

    /// <summary>
    /// Despite being "valid", the validation can add a list of warnings to the user.
    /// If this is not yet supported by the cluster, the field is ignored.
    /// Warnings may contain up to 256 characters but they should be limited to 120 characters.
    /// If more than 4096 characters are submitted, additional messages are ignored.
    /// </summary>
    public IList<string> Warnings { get; init; } = new List<string>();

    /// <inheritdoc/>
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        if (string.IsNullOrWhiteSpace(Uid))
        {
            response.StatusCode = StatusCodes.Status500InternalServerError;
            await response.WriteAsync("No request UID was provided.");
            return;
        }

        if (ModifiedObject is not null && OriginalObject is null)
        {
            response.StatusCode = StatusCodes.Status500InternalServerError;
            await response.WriteAsync("No original object was provided.");
            return;
        }

        await response.WriteAsJsonAsync(
            new AdmissionResponse
            {
                Response = new()
                {
                    Uid = Uid,
                    Allowed = Valid,
                    Status = Status,
                    Warnings = Warnings.ToArray(),
                    PatchType = ModifiedObject is null ? null : JsonPatch,
                    Patch = ModifiedObject is null ? null : OriginalObject!.Base64Diff(ModifiedObject),
                },
            },
            AdmissionResponse.SerializerOptions);
    }
}
