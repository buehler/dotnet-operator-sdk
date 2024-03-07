using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Http;

namespace KubeOps.Operator.Web.Webhooks.Admission;

/// <summary>
/// The admission status for the response to the API.
/// </summary>
/// <param name="Message">A message that is passed to the API.</param>
/// <param name="Code">A custom status code to provide more detailed information.</param>
public record AdmissionStatus([property: JsonPropertyName("message")]
    string Message, [property: JsonPropertyName("code")]
    int? Code = StatusCodes.Status200OK);
