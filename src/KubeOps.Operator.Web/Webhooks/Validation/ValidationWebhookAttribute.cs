using Microsoft.AspNetCore.Mvc;

namespace KubeOps.Operator.Web.Webhooks.Validation;

/// <summary>
/// Defines an MVC controller as "validation webhook". The route is automatically set to
/// <c>/validate/[lower-case-name-of-the-type]</c>.
/// This must be used in conjunction with the <see cref="ValidationWebhook{TEntity}"/> class.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ValidationWebhookAttribute : RouteAttribute
{
    public ValidationWebhookAttribute(Type entityType)
        : base($"/validate/{entityType.Name.ToLowerInvariant()}")
    {
    }
}
