using KubeOps.Transpiler;

using Microsoft.AspNetCore.Mvc;

namespace KubeOps.Operator.Web.Webhooks.Conversion;

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ConversionWebhookAttribute : RouteAttribute
{
    public ConversionWebhookAttribute(Type entityType)
        : base(GetRouteTemplate(entityType))
    {
    }

    public ConversionWebhookAttribute(string group, string pluralName)
        : base($"/convert/{group}/{pluralName}")
    {
    }

    private static string GetRouteTemplate(Type entityType)
    {
        var meta = Entities.ToEntityMetadata(entityType).Metadata;
        return $"/convert/{meta.Group}/{meta.PluralName}";
    }
}
