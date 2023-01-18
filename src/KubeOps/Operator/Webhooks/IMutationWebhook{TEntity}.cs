using System.Text;

namespace KubeOps.Operator.Webhooks;

/// <summary>
/// <para>
/// Mutation webhook for kubernetes.
/// This is used by the https://kubernetes.io/docs/reference/access-authn-authz/extensible-admission-controllers/.
/// Implement a class with this interface, overwrite the needed functions.
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
/// Overwrite the needed ones as defined in <see cref="IAdmissionWebhook{TEntity,TResult}.Operations"/>.
/// The async implementations take precedence over the synchronous ones.
/// </para>
/// <para>
/// Note that the operator must use HTTPS (in some form) to use mutators.
/// For local development, this could be done with ngrok (https://ngrok.com/).
/// </para>
/// <para>
/// The operator generator (if enabled during build) will generate the CA certificate
/// and the operator will generate the server certificate during pod startup.
/// </para>
/// </summary>
/// <typeparam name="TEntity">The type of the entity that should be validated.</typeparam>
public interface IMutationWebhook<TEntity> : IAdmissionWebhook<TEntity, MutationResult>
{
    /// <inheritdoc />
    string IAdmissionWebhook<TEntity, MutationResult>.Endpoint
    {
        get => WebhookEndpointFactory.Create<TEntity>(GetType(), "/mutate");
    }

    /// <inheritdoc />
    MutationResult IAdmissionWebhook<TEntity, MutationResult>.Create(TEntity newEntity, bool dryRun)
        => AdmissionResult.NotImplemented<MutationResult>();

    /// <inheritdoc />
    MutationResult IAdmissionWebhook<TEntity, MutationResult>.Update(
        TEntity oldEntity,
        TEntity newEntity,
        bool dryRun)
        => AdmissionResult.NotImplemented<MutationResult>();

    /// <inheritdoc />
    MutationResult IAdmissionWebhook<TEntity, MutationResult>.Delete(TEntity oldEntity, bool dryRun)
        => AdmissionResult.NotImplemented<MutationResult>();

    AdmissionResponse IAdmissionWebhook<TEntity, MutationResult>.TransformResult(
        MutationResult result,
        AdmissionRequest<TEntity> request)
    {
        var response = new AdmissionResponse
        {
            Allowed = result.Valid,
            Status = result.StatusMessage == null
                ? null
                : new AdmissionResponse.Reason { Code = result.StatusCode ?? 0, Message = result.StatusMessage, },
            Warnings = result.Warnings.ToArray(),
        };

        if (result.ModifiedObject != null)
        {
            var from = request.Operation == "DELETE"
                ? request.OldObject
                : request.Object;
            var to = result.ModifiedObject;

            var patch = KubernetesJsonDiffer.DiffObjects(from, to);
            response.Patch = Convert.ToBase64String(Encoding.UTF8.GetBytes(patch.ToString()));
        }

        return response;
    }
}
