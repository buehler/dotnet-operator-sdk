using Microsoft.AspNetCore.Mvc;

namespace KubeOps.Operator.Web.Webhooks.Validation;

public class ValidationWebhookAttribute : RouteAttribute
{
    public ValidationWebhookAttribute(Type entityType)
        : base($"/validate/{entityType.Name.ToLowerInvariant()}")
    {
    }
}
