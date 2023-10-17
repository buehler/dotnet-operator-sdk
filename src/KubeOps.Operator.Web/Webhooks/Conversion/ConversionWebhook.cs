using System.Text.Json;

using k8s;
using k8s.Models;

using KubeOps.Transpiler;

using Microsoft.AspNetCore.Mvc;

namespace KubeOps.Operator.Web.Webhooks.Conversion;

[ApiController]
public abstract class ConversionWebhook : ControllerBase
{
    private readonly Dictionary<string,
        Dictionary<string, (Func<object, object> Converter, Type From)>> _converters =
        new();

    static ConversionWebhook()
    {
        KubernetesJson.AddJsonOptions(c => SerializerOptions = c);
    }

    private static JsonSerializerOptions SerializerOptions { get; set; } = null!;

    [HttpPost]
    public IActionResult Convert([FromBody] ConversionRequest request)
    {
        try
        {
            var toConverters = _converters[request.Request.DesiredApiVersion];
            var results = new List<object>();
            foreach (var obj in request.Request.Objects)
            {
                if (obj["apiVersion"]?.GetValue<string>() is not { } targetApiVersion ||
                    !toConverters.ContainsKey(targetApiVersion))
                {
                    continue;
                }

                var (converter, from) = toConverters[targetApiVersion];
                results.Add(converter(obj.Deserialize(from, SerializerOptions)!));
            }

            return new ConversionResponse(request.Request.Uid, results);
        }
        catch (Exception e)
        {
            return new ConversionResponse(request.Request.Uid, e.ToString());
        }
    }

    protected void RegisterConverter<TFrom, TTo>(Func<TFrom, TTo> converter)
        where TFrom : IKubernetesObject<V1ObjectMeta>
        where TTo : IKubernetesObject<V1ObjectMeta>
    {
        var toMeta = Entities.ToEntityMetadata<TTo>().Metadata;
        var fromMeta = Entities.ToEntityMetadata<TFrom>().Metadata;

        if (!_converters.ContainsKey(toMeta.GroupWithVersion))
        {
            _converters[toMeta.GroupWithVersion] = new();
        }

        _converters[toMeta.GroupWithVersion][fromMeta.GroupWithVersion] = (o => converter((TFrom)o), typeof(TFrom));
    }
}
