using System.Text.Json.Serialization;

using k8s;

using KubeOps.Operator.Web.Webhooks.Admission;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KubeOps.Operator.Web.Webhooks.Conversion;

internal sealed class ConversionResponse : ConversionReview, IActionResult
{
    public ConversionResponse(string uid, string errorMessage)
    {
        Response.Uid = uid;
        Response.Result = new(errorMessage);
    }

    public ConversionResponse(string uid, IEnumerable<object> result)
    {
        Response.Uid = uid;
        Response.ConvertedObjects = result.ToArray();
    }

    [JsonPropertyName("response")]
    public ConversionRequestData Response { get; init; } = new();

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        if (string.IsNullOrWhiteSpace(Response.Uid))
        {
            response.StatusCode = StatusCodes.Status500InternalServerError;
            await response.WriteAsync("No request UID was provided.");
            return;
        }

        response.ContentType = "application/json; charset=utf-8";
        await response.WriteAsync(KubernetesJson.Serialize(this));
    }

    internal sealed class ConversionRequestData
    {
        /// <summary>
        /// The unique ID of the conversion request.
        /// </summary>
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public ConversionStatus Result { get; set; } = new();

        /// <summary>
        /// TODO
        /// </summary>
        [JsonPropertyName("convertedObjects")]
        public object[] ConvertedObjects { get; set; } = Array.Empty<object>();
    }
}
