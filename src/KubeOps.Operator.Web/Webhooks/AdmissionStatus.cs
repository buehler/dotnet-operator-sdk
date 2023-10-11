using Microsoft.AspNetCore.Http;

namespace KubeOps.Operator.Web.Webhooks;

/// <summary>
/// The admission status for the response to the API.
/// </summary>
/// <param name="Message">A message that is passed to the API.</param>
/// <param name="Code">A custom status code to provide more detailed information.</param>
public record AdmissionStatus(string Message, int? Code = StatusCodes.Status200OK);
