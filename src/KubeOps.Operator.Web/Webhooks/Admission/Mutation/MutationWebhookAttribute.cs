using Microsoft.AspNetCore.Mvc;

namespace KubeOps.Operator.Web.Webhooks.Admission.Mutation;

/// <summary>
/// Defines an MVC controller as "mutation webhook". The route is automatically set to
/// <c>/mutate/[lower-case-name-of-the-type]</c>.
/// This must be used in conjunction with the <see cref="MutationWebhook{TEntity}"/> class.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class MutationWebhookAttribute(Type entityType) : RouteAttribute($"/mutate/{entityType.Name.ToLowerInvariant()}");
