using System.Text.Json;

using k8s;

using Microsoft.AspNetCore.Mvc;

namespace KubeOps.Operator.Web.Webhooks.Conversion;

[ApiController]
public abstract class ConversionWebhook : ControllerBase
{
    static ConversionWebhook()
    {
        KubernetesJson.AddJsonOptions(c => SerializerOptions = c);
    }

    protected abstract IEntityConverter[] Converters { get; }

    private static JsonSerializerOptions SerializerOptions { get; set; } = null!;

    private IEnumerable<(string To, string From, Func<object, object> Converter, Type FromType)> AvailableConversions =>
        Converters
            .SelectMany(c => new (string, string, Func<object, object>, Type)[]
            {
                (c.ToGroupVersion, c.FromGroupVersion, c.Convert, c.FromType),
                (c.FromGroupVersion, c.ToGroupVersion, c.Revert, c.ToType),
            });

    [HttpPost]
    public IActionResult Convert([FromBody] ConversionRequest request)
    {
        try
        {
            var toConverters = AvailableConversions
                .Where(c => c.To == request.Request.DesiredApiVersion)
                .ToList();
            var results = new List<object>();
            foreach (var obj in request.Request.Objects)
            {
                if (obj["apiVersion"]?.GetValue<string>() is not { } targetApiVersion ||
                    toConverters.TrueForAll(c => c.From != targetApiVersion))
                {
                    continue;
                }

                var (_, _, converter, type) = toConverters.Find(c => c.From == targetApiVersion);

                results.Add(converter(obj.Deserialize(type, SerializerOptions)!));
            }

            return new ConversionResponse(request.Request.Uid, results);
        }
        catch (Exception e)
        {
            return new ConversionResponse(request.Request.Uid, e.ToString());
        }
    }
}
